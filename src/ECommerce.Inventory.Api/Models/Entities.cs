namespace ECommerce.Inventory.Api.Models;

/// <summary>
/// Represents inventory for a product.
/// </summary>
public class InventoryItem
{
    public int ProductId { get; set; }
    public int Stock { get; set; }
    public int ReservedStock { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public int AvailableStock => Stock - ReservedStock;
}

/// <summary>
/// Represents a reservation of inventory.
/// </summary>
public class InventoryReservation
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int? OrderId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
