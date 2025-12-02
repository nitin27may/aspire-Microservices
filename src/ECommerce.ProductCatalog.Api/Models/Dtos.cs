namespace ECommerce.ProductCatalog.Api.Models;

/// <summary>
/// DTO for product response.
/// </summary>
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

/// <summary>
/// DTO for category response.
/// </summary>
public record CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public int ProductCount { get; init; }
}

/// <summary>
/// Request for paginated product list.
/// </summary>
public record ProductsRequest
{
    public int? CategoryId { get; init; }
    public string? Search { get; init; }
    public string? SortBy { get; init; } // "price_asc", "price_desc", "name_asc"
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

/// <summary>
/// Paginated response wrapper.
/// </summary>
public record PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
