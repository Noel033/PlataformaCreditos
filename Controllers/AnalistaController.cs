using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.SignalR;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.Hubs;

namespace PlataformaCreditos.Controllers;

[Authorize(Roles = "Analista")]
public class AnalistaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IHubContext<SolicitudesHub> _hubContext;

    public AnalistaController(ApplicationDbContext context, IDistributedCache cache, IHubContext<SolicitudesHub> hubContext)
    {
        _context = context;
        _cache = cache;
        _hubContext = hubContext;
    }

    public async Task<IActionResult> Index()
    {
        var solicitudes = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .ThenInclude(c => c.Usuario)
            .Where(s => s.Estado == EstadoSolicitud.Pendiente)
            .OrderBy(s => s.FechaSolicitud)
            .ToListAsync();

        return View(solicitudes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
            return NotFound();

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "La solicitud no está en estado Pendiente.";
            return RedirectToAction(nameof(Index));
        }

        if (solicitud.MontoSolicitado > solicitud.Cliente!.IngresosMensuales * 5)
        {
            TempData["Error"] = "El monto solicitado excede 5 veces los ingresos mensuales del cliente. No se puede aprobar.";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = EstadoSolicitud.Aprobado;
        await _context.SaveChangesAsync();

        if (solicitud.Cliente!.UsuarioId != null)
        {
            await _cache.RemoveAsync($"solicitudes_{solicitud.Cliente.UsuarioId}");
            await _hubContext.Clients.User(solicitud.Cliente.UsuarioId).SendAsync("SolicitudEstadoActualizado", new { solicitudId = solicitud.Id, estado = solicitud.Estado.ToString(), motivoRechazo = solicitud.MotivoRechazo });
        }

        TempData["Success"] = $"Solicitud #{id} aprobada con éxito.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, string motivoRechazo)
    {
        if (string.IsNullOrWhiteSpace(motivoRechazo))
        {
            TempData["Error"] = "El motivo de rechazo es obligatorio.";
            return RedirectToAction(nameof(Index));
        }

        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
            return NotFound();

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "La solicitud no está en estado Pendiente.";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = EstadoSolicitud.Rechazado;
        solicitud.MotivoRechazo = motivoRechazo;
        await _context.SaveChangesAsync();

        if (solicitud.Cliente!.UsuarioId != null)
        {
            await _cache.RemoveAsync($"solicitudes_{solicitud.Cliente.UsuarioId}");
            await _hubContext.Clients.User(solicitud.Cliente.UsuarioId).SendAsync("SolicitudEstadoActualizado", new { solicitudId = solicitud.Id, estado = solicitud.Estado.ToString(), motivoRechazo = solicitud.MotivoRechazo });
        }

        TempData["Success"] = $"Solicitud #{id} rechazada.";
        return RedirectToAction(nameof(Index));
    }
}
