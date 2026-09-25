using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Services;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumerService(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("RabbitMQ:ConsumerEnabled");
        if (!enabled) return Task.CompletedTask;

        var factory = new ConnectionFactory { Uri = new Uri(_configuration["RabbitMQ:ConnectionString"]!), DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        var queueName = _configuration["RabbitMQ:QueueName"]!;
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var evento = JsonSerializer.Deserialize<JsonElement>(message);

            var messageId = evento.GetProperty("MessageId").GetString();
            var solicitudId = evento.GetProperty("SolicitudId").GetInt32();
            var usuarioId = evento.GetProperty("UsuarioId").GetString();

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var existe = dbContext.Notificaciones.Any(n => n.MessageId == messageId);
                if (!existe)
                {
                    var notificacion = new Notificacion
                    {
                        MessageId = messageId!,
                        SolicitudId = solicitudId,
                        UsuarioId = usuarioId!,
                        Texto = "Recibimos tu solicitud de crédito y está pendiente de evaluación",
                        FechaProcesamientoUtc = DateTime.UtcNow
                    };

                    dbContext.Notificaciones.Add(notificacion);
                    await dbContext.SaveChangesAsync();
                }
            }

            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        return base.StopAsync(cancellationToken);
    }
}
