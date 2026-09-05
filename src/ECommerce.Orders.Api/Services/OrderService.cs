using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ECommerce.Orders.Api.Data;
using ECommerce.Orders.Api.Models;
using ECommerce.Shared.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace ECommerce.Orders.Api.Services;

/// <summary>
/// Service for managing orders.
/// </summary>
public class OrderService
{
    private readonly OrdersDbContext _dbContext;
    private readonly IConnection _rabbitConnection;
    private readonly HttpClient _inventoryClient;
    private readonly HttpClient _userClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrdersDbContext dbContext,
        IConnection rabbitConnection,
        IHttpClientFactory httpClientFactory,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _rabbitConnection = rabbitConnection;
        _inventoryClient = httpClientFactory.CreateClient("inventoryapi");
        _userClient = httpClientFactory.CreateClient("usermanagementapi");
        _logger = logger;
    }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    public async Task<(bool Success, string? Error, OrderDto? Order)> CreateOrderAsync(string userId, CreateOrderRequest request)
    {
        // Step 1: Get user email for notifications
        string userEmail = "";
        try
        {
            var userResponse = await _userClient.GetAsync($"/api/users/{userId}");
            if (userResponse.IsSuccessStatusCode)
            {
                var userProfile = await userResponse.Content.ReadFromJsonAsync<UserProfileDto>();
                userEmail = userProfile?.Email ?? "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch user profile for {UserId}", userId);
        }

        // Step 2: Reserve inventory
        var reserveRequest = new
        {
            Items = request.Items.Select(i => new { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
        };

        try
        {
            var reserveResponse = await _inventoryClient.PostAsJsonAsync("/api/inventory/reserve", reserveRequest);
            if (!reserveResponse.IsSuccessStatusCode)
            {
                var error = await reserveResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Inventory reservation failed: {Error}", error);
                return (false, "Unable to reserve inventory. Some items may be out of stock.", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling inventory service");
            return (false, "Unable to verify inventory availability.", null);
        }

        // Step 3: Create order
        var orderNumber = GenerateOrderNumber();
        var totalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            TotalAmount = totalAmount,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        // Step 4: Publish order created event
        await PublishOrderCreatedEventAsync(order, userEmail);

        // Step 5: Update order status to Confirmed
        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order created: {OrderNumber}", orderNumber);

        return (true, null, MapToDto(order));
    }

    /// <summary>
    /// Gets an order by its number.
    /// </summary>
    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        return order == null ? null : MapToDto(order);
    }

    /// <summary>
    /// Gets all orders for a user.
    /// </summary>
    public async Task<PaginatedOrdersResponse> GetOrdersByUserAsync(string userId, int page = 1, int pageSize = 10)
    {
        var query = _dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedOrdersResponse
        {
            Items = orders.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return order == null ? null : MapToDto(order);
    }

    private async Task PublishOrderCreatedEventAsync(Order order, string userEmail)
    {
        try
        {
            await using var channel = await _rabbitConnection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: "orders.exchange",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var orderEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                UserEmail = userEmail,
                Items = order.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList(),
                TotalAmount = order.TotalAmount,
                Timestamp = DateTime.UtcNow
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(orderEvent));

            await channel.BasicPublishAsync(
                exchange: "orders.exchange",
                routingKey: "order.created",
                body: body);

            _logger.LogInformation("Published OrderCreatedEvent for order: {OrderNumber}", order.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish order event for: {OrderNumber}", order.OrderNumber);
        }
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            ShippingAddress = order.ShippingAddress,
            City = order.City,
            PostalCode = order.PostalCode,
            Country = order.Country,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }
}

// DTOs for service communication
internal record UserProfileDto
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
