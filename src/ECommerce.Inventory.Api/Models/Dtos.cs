namespace ECommerce.Inventory.Api.Models;

/// <summary>
/// DTO for inventory status.
/// </summary>
public record InventoryDto
{
    public int ProductId { get; init; }
    public int Stock { get; init; }
    public int ReservedStock { get; init; }
    public int AvailableStock { get; init; }
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Request to reserve inventory.
/// </summary>
public record ReserveInventoryRequest
{
    public int? OrderId { get; init; }
    public List<ReserveItem> Items { get; init; } = [];
}

/// <summary>
/// Item to reserve.
/// </summary>
public record ReserveItem
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// Response for reservation request.
/// </summary>
public record ReservationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int? ReservationId { get; init; }
}

/// <summary>
/// Request to check stock availability.
/// </summary>
public record StockCheckRequest
{
    public List<ReserveItem> Items { get; init; } = [];
}

/// <summary>
/// Response for stock check.
/// </summary>
public record StockCheckResponse
{
    public bool AllAvailable { get; init; }
    public List<StockCheckItem> Items { get; init; } = [];
}

/// <summary>
/// Individual item stock check result.
/// </summary>
public record StockCheckItem
{
    public int ProductId { get; init; }
    public int RequestedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public bool IsAvailable { get; init; }
}
