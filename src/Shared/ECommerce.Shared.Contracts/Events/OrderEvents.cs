namespace ECommerce.Shared.Contracts.Events;

/// <summary>
/// Event published when a new order is created.
/// </summary>
public record OrderCreatedEvent
{
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public List<OrderItemEvent> Items { get; init; } = [];
    public decimal TotalAmount { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Order item details for the event.
/// </summary>
public record OrderItemEvent
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

/// <summary>
/// Event published when inventory is updated.
/// </summary>
public record InventoryUpdatedEvent
{
    public int ProductId { get; init; }
    public int NewStock { get; init; }
    public DateTime Timestamp { get; init; }
}
