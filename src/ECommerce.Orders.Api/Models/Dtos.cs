using System.ComponentModel.DataAnnotations;

namespace ECommerce.Orders.Api.Models;

/// <summary>
/// Request to create a new order.
/// </summary>
public record CreateOrderRequest
{
    [Required]
    public List<CreateOrderItemRequest> Items { get; init; } = [];
    
    [Required]
    public string ShippingAddress { get; init; } = string.Empty;
    
    [Required]
    public string City { get; init; } = string.Empty;
    
    [Required]
    public string PostalCode { get; init; } = string.Empty;
    
    [Required]
    public string Country { get; init; } = string.Empty;
}

/// <summary>
/// Item in order creation request.
/// </summary>
public record CreateOrderItemRequest
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

/// <summary>
/// DTO for order response.
/// </summary>
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

/// <summary>
/// DTO for order item response.
/// </summary>
public record OrderItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal => Quantity * UnitPrice;
}

/// <summary>
/// Paginated orders response.
/// </summary>
public record PaginatedOrdersResponse
{
    public IEnumerable<OrderDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
