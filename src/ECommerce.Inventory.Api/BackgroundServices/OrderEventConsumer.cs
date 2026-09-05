using System.Text;
using System.Text.Json;
using ECommerce.Inventory.Api.Models;
using ECommerce.Inventory.Api.Services;
using ECommerce.Shared.Contracts.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Inventory.Api.BackgroundServices;

/// <summary>
/// Background service that consumes order events and deducts inventory.
/// </summary>
public class OrderEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _rabbitConnection;
    private readonly ILogger<OrderEventConsumer> _logger;

    public OrderEventConsumer(
        IServiceProvider serviceProvider,
        IConnection rabbitConnection,
        ILogger<OrderEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitConnection = rabbitConnection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderEventConsumer starting...");

        var channel = await _rabbitConnection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: "orders.exchange",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        await channel.QueueDeclareAsync(
            queue: "orders.inventory",
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: "orders.inventory",
            exchange: "orders.exchange",
            routingKey: "order.created");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, args) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Body.ToArray());
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(body);

                if (orderEvent != null)
                {
                    _logger.LogInformation("Processing order event: {OrderId}", orderEvent.OrderId);

                    using var scope = _serviceProvider.CreateScope();
                    var inventoryService = scope.ServiceProvider.GetRequiredService<InventoryService>();

                    var items = orderEvent.Items.Select(i => new ReserveItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    }).ToList();

                    await inventoryService.DeductInventoryAsync(orderEvent.OrderId, items);
                }

                await channel.BasicAckAsync(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order event");
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: "orders.inventory",
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("OrderEventConsumer started, waiting for messages...");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            await channel.CloseAsync();
            await channel.DisposeAsync();
        }
    }
}
