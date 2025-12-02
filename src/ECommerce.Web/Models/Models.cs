namespace ECommerce.Web.Models;

// Product models
public record ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public int ProductCount { get; init; }
}

public record PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// Auth models
public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string ShippingAddress { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}

public record AuthResponse
{
    public string Token { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public record UserProfileDto
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string ShippingAddress { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}

// Cart models
public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal => Price * Quantity;
}

public class ShoppingCart
{
    public List<CartItem> Items { get; set; } = [];
    public decimal Subtotal => Items.Sum(i => i.Subtotal);
    public decimal Tax => Subtotal * 0.13m; // 13% HST
    public decimal Total => Subtotal + Tax;
    public int ItemCount => Items.Sum(i => i.Quantity);
}

// Order models
public record CreateOrderRequest
{
    public List<CreateOrderItemRequest> Items { get; init; } = [];
    public string ShippingAddress { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}

public record CreateOrderItemRequest
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public record OrderDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string ShippingAddress { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<OrderItemDto> Items { get; init; } = [];
}

public record OrderItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal => Quantity * UnitPrice;
}

public record PaginatedOrdersResponse
{
    public IEnumerable<OrderDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// Inventory models
public record InventoryDto
{
    public int ProductId { get; init; }
    public int Stock { get; init; }
    public int ReservedStock { get; init; }
    public int AvailableStock { get; init; }
    public DateTime LastUpdated { get; init; }
}

public record StockCheckRequest
{
    public List<StockCheckRequestItem> Items { get; init; } = [];
}

public record StockCheckRequestItem
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
}

public record StockCheckResponse
{
    public bool AllAvailable { get; init; }
    public List<StockCheckItem> Items { get; init; } = [];
}

public record StockCheckItem
{
    public int ProductId { get; init; }
    public int RequestedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public bool IsAvailable { get; init; }
}
