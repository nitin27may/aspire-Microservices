using ECommerce.Web.Models;

namespace ECommerce.Web.Services;

/// <summary>
/// Service for communicating with the Product Catalog API.
/// </summary>
public class ProductService
{
    private readonly HttpClient _httpClient;

    public ProductService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        return response ?? [];
    }

    public async Task<PaginatedResponse<ProductDto>> GetProductsAsync(int? categoryId = null, string? search = null, string? sortBy = null, int page = 1, int pageSize = 10)
    {
        var url = $"/api/products?page={page}&pageSize={pageSize}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrEmpty(sortBy)) url += $"&sortBy={sortBy}";

        var response = await _httpClient.GetFromJsonAsync<PaginatedResponse<ProductDto>>(url);
        return response ?? new PaginatedResponse<ProductDto>();
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<ProductDto>($"/api/products/{id}");
    }

    public async Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<ProductDto>>("/api/products/featured");
        return response ?? [];
    }
}
