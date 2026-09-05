using System.Text.Json;
using ECommerce.ProductCatalog.Api.Data;
using ECommerce.ProductCatalog.Api.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerce.ProductCatalog.Api.Services;

/// <summary>
/// Service for managing product catalog operations with Redis caching.
/// </summary>
public class ProductCatalogService
{
    private readonly CatalogDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ProductCatalogService> _logger;
    private const int CacheTtlMinutes = 10;
    private const int ProductCacheTtlMinutes = 30;

    public ProductCatalogService(
        CatalogDbContext dbContext,
        IConnectionMultiplexer redis,
        ILogger<ProductCatalogService> logger)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Gets all categories with product count.
    /// </summary>
    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        return await _dbContext.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync();
    }

    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        return await _dbContext.Categories
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets paginated products with optional filtering and sorting.
    /// </summary>
    public async Task<PaginatedResponse<ProductDto>> GetProductsAsync(ProductsRequest request)
    {
        var cacheKey = $"products:page:{request.Page}:size:{request.PageSize}:cat:{request.CategoryId ?? 0}:search:{request.Search ?? ""}:sort:{request.SortBy ?? ""}";

        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(cacheKey);

        if (!cached.IsNullOrEmpty)
        {
            _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<PaginatedResponse<ProductDto>>(cached.ToString())!;
        }

        _logger.LogInformation("Cache miss for key: {CacheKey}", cacheKey);

        var query = _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Description.ToLower().Contains(search));
        }

        query = request.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category!.Name,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var response = new PaginatedResponse<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(response), TimeSpan.FromMinutes(CacheTtlMinutes));

        return response;
    }

    /// <summary>
    /// Gets a single product by ID.
    /// </summary>
    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var cacheKey = $"product:{id}";
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(cacheKey);

        if (!cached.IsNullOrEmpty)
        {
            _logger.LogInformation("Cache hit for product: {ProductId}", id);
            return JsonSerializer.Deserialize<ProductDto>(cached.ToString());
        }

        _logger.LogInformation("Cache miss for product: {ProductId}", id);

        var product = await _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category!.Name,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (product != null)
        {
            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(product), TimeSpan.FromMinutes(ProductCacheTtlMinutes));
        }

        return product;
    }

    /// <summary>
    /// Gets featured products (latest 8).
    /// </summary>
    public async Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync()
    {
        var cacheKey = "products:featured";
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(cacheKey);

        if (!cached.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<List<ProductDto>>(cached.ToString())!;
        }

        var products = await _dbContext.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category!.Name,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(products), TimeSpan.FromMinutes(CacheTtlMinutes));

        return products;
    }
}
