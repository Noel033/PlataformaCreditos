using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using System.Security.Claims;

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;

namespace PlataformaCreditos.Controllers;

[Authorize]
public class SolicitudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;

    public SolicitudController(ApplicationDbContext context, IDistributedCache cache, IConfiguration configuration)
    {
        _context = context;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(EstadoSolicitud? estado, decimal? montoMinimo, decimal? montoMaximo, DateTime? fechaInicio, DateTime? fechaFin)
    {
        if (montoMinimo.HasValue && montoMinimo < 0)
            ModelState.AddModelError("", "El monto mínimo no puede ser negativo.");
            
        if (montoMaximo.HasValue && montoMaximo < 0)
            ModelState.AddModelError("", "El monto máximo no puede ser negativo.");

        if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio > fechaFin)
            ModelState.AddModelError("", "La fecha de inicio no puede ser mayor a la fecha de fin.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);
        
        if (cliente == null) 
        {
            return View(new List<SolicitudCredito>());
        }

        var cacheKey = $"solicitudes_{userId}";
        var cachedData = await _cache.GetStringAsync(cacheKey);
        List<SolicitudCredito> allSolicitudes;

        if (!string.IsNullOrEmpty(cachedData))
        {
            allSolicitudes = JsonSerializer.Deserialize<List<SolicitudCredito>>(cachedData, new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles }) ?? new List<SolicitudCredito>();
        }
        else
        {
            allSolicitudes = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.ClienteId == cliente.Id)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            };

            var jsonOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(allSolicitudes, jsonOptions), cacheOptions);
        }

        var query = allSolicitudes.AsEnumerable();

        if (ModelState.IsValid)
        {
            if (estado.HasValue)
                query = query.Where(s => s.Estado == estado.Value);

            if (montoMinimo.HasValue)
                query = query.Where(s => s.MontoSolicitado >= montoMinimo.Value);

            if (montoMaximo.HasValue)
                query = query.Where(s => s.MontoSolicitado <= montoMaximo.Value);

            if (fechaInicio.HasValue)
                query = query.Where(s => s.FechaSolicitud >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(s => s.FechaSolicitud <= fechaFin.Value);
        }

        ViewBag.Estado = estado;
        ViewBag.MontoMinimo = montoMinimo;
        ViewBag.MontoMaximo = montoMaximo;
        ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

        return View(query.ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

        if (cliente == null)
            return NotFound();

        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id && s.ClienteId == cliente.Id);

        if (solicitud == null)
            return NotFound();

        HttpContext.Session.SetString("UltimaSolicitud", solicitud.MontoSolicitado.ToString("C"));

        return View(solicitud);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(decimal montoSolicitado)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

        if (cliente == null)
        {
            return NotFound("Cliente no encontrado.");
        }

        if (!cliente.Activo)
        {
            ModelState.AddModelError("", "El cliente asociado no está activo.");
        }

        var tienePendiente = await _context.SolicitudesCredito
            .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);
        
        if (tienePendiente)
        {
            ModelState.AddModelError("", "Ya tienes una solicitud en estado Pendiente. No puedes crear otra.");
        }

        if (montoSolicitado > cliente.IngresosMensuales * 10)
        {
            ModelState.AddModelError("montoSolicitado", $"El monto solicitado no puede superar 10 veces tus ingresos mensuales ({(cliente.IngresosMensuales * 10).ToString("C")}).");
        }
        
        if (montoSolicitado <= 0)
        {
            ModelState.AddModelError("montoSolicitado", "El monto solicitado debe ser mayor a 0.");
        }

        if (!ModelState.IsValid)
        {
            return View(montoSolicitado);
        }

        var nuevaSolicitud = new SolicitudCredito
        {
            ClienteId = cliente.Id,
            MontoSolicitado = montoSolicitado,
            FechaSolicitud = DateTime.UtcNow,
            Estado = EstadoSolicitud.Pendiente
        };

        _context.SolicitudesCredito.Add(nuevaSolicitud);
        await _context.SaveChangesAsync();

        var cacheKey = $"solicitudes_{userId}";
        await _cache.RemoveAsync(cacheKey);

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(_configuration["RabbitMQ:ConnectionString"]!) };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            
            var queueName = _configuration["RabbitMQ:QueueName"]!;
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            
            channel.ConfirmSelect();
            
            var evento = new {
                MessageId = Guid.NewGuid().ToString(),
                SolicitudId = nuevaSolicitud.Id,
                UsuarioId = userId,
                FechaEventoUtc = DateTime.UtcNow
            };
            
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento));
            
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            
            channel.BasicPublish(exchange: string.Empty, routingKey: queueName, mandatory: true, basicProperties: properties, body: body);
            
            channel.WaitForConfirmsOrDie();
        }
        catch (Exception)
        {
            // Error publishing, but do not delete request
        }

        TempData["Success"] = "Solicitud de crédito creada exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> MisNotificaciones()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notificaciones = await _context.Notificaciones
            .Where(n => n.UsuarioId == userId)
            .OrderByDescending(n => n.FechaProcesamientoUtc)
            .ToListAsync();

        return View(notificaciones);
    }
}
