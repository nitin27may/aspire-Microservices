using System.Text;
using System.Text.Json;
using ECommerce.Notifications.Api.Services;
using ECommerce.Shared.Contracts.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Notifications.Api.BackgroundServices;

/// <summary>
/// Background service that consumes order events and sends notifications.
/// </summary>
public class NotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _rabbitConnection;
    private readonly ILogger<NotificationConsumer> _logger;

    public NotificationConsumer(
        IServiceProvider serviceProvider,
        IConnection rabbitConnection,
        ILogger<NotificationConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitConnection = rabbitConnection;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationConsumer starting...");

        var channel = _rabbitConnection.CreateModel();
        
        // Declare exchange
        channel.ExchangeDeclare(
            exchange: "orders.exchange",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Declare queue
        channel.QueueDeclare(
            queue: "orders.notifications",
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Bind queue to exchange
        channel.QueueBind(
            queue: "orders.notifications",
            exchange: "orders.exchange",
            routingKey: "order.created");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (sender, args) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Body.ToArray());
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(body);

                if (orderEvent != null && !string.IsNullOrEmpty(orderEvent.UserEmail))
                {
                    _logger.LogInformation("Processing notification for order: {OrderId}", orderEvent.OrderId);

                    using var scope = _serviceProvider.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                    emailService.SendOrderConfirmationAsync(orderEvent).GetAwaiter().GetResult();
                }

                channel.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification event");
                channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(
            queue: "orders.notifications",
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("NotificationConsumer started, waiting for messages...");

        stoppingToken.Register(() =>
        {
            channel.Close();
            channel.Dispose();
        });

        return Task.CompletedTask;
    }
}
