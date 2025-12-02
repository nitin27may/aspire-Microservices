using ECommerce.ProductCatalog.Api.Data;
using ECommerce.ProductCatalog.Api.Models;
using ECommerce.ProductCatalog.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

builder.AddRedisClient("redis");

builder.Services.AddScoped<ProductCatalogService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    
    // Redirect root to Scalar API docs
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    // Seed categories if empty
    if (!context.Categories.Any())
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Electronics", Description = "Latest gadgets and tech products", DisplayOrder = 1 },
            new() { Id = 2, Name = "Clothing", Description = "Fashion and apparel for everyone", DisplayOrder = 2 },
            new() { Id = 3, Name = "Home & Garden", Description = "Everything for your home", DisplayOrder = 3 },
            new() { Id = 4, Name = "Sports & Outdoors", Description = "Gear for active lifestyles", DisplayOrder = 4 },
            new() { Id = 5, Name = "Books", Description = "Physical and digital reading materials", DisplayOrder = 5 }
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
    }
    
    // Seed products if empty
    if (!context.Products.Any())
    {
        var products = new List<Product>
        {
            // Electronics
            new() { Id = 1, Name = "Wireless Headphones", Description = "Premium wireless headphones with noise cancellation", Price = 99.99m, CategoryId = 1, ImageUrl = "https://placehold.co/300x300/0066cc/ffffff?text=Headphones", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Smart Watch", Description = "Advanced smartwatch with health tracking features", Price = 249.99m, CategoryId = 1, ImageUrl = "https://placehold.co/300x300/0066cc/ffffff?text=Smart+Watch", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "Laptop Stand", Description = "Ergonomic aluminum laptop stand for better posture", Price = 45.99m, CategoryId = 1, ImageUrl = "https://placehold.co/300x300/0066cc/ffffff?text=Laptop+Stand", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 4, Name = "USB-C Hub", Description = "Multi-port USB-C hub with HDMI and SD card slots", Price = 34.99m, CategoryId = 1, ImageUrl = "https://placehold.co/300x300/0066cc/ffffff?text=USB+Hub", IsActive = true, CreatedAt = DateTime.UtcNow },
            // Clothing
            new() { Id = 5, Name = "Classic T-Shirt", Description = "Comfortable cotton t-shirt in various colors", Price = 19.99m, CategoryId = 2, ImageUrl = "https://placehold.co/300x300/28a745/ffffff?text=T-Shirt", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 6, Name = "Denim Jeans", Description = "Classic fit denim jeans with stretch comfort", Price = 59.99m, CategoryId = 2, ImageUrl = "https://placehold.co/300x300/28a745/ffffff?text=Jeans", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 7, Name = "Running Shoes", Description = "Lightweight running shoes with cushioned sole", Price = 89.99m, CategoryId = 2, ImageUrl = "https://placehold.co/300x300/28a745/ffffff?text=Shoes", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 8, Name = "Winter Jacket", Description = "Warm winter jacket with water-resistant exterior", Price = 129.99m, CategoryId = 2, ImageUrl = "https://placehold.co/300x300/28a745/ffffff?text=Jacket", IsActive = true, CreatedAt = DateTime.UtcNow },
            // Home & Garden
            new() { Id = 9, Name = "Coffee Maker", Description = "Programmable coffee maker with thermal carafe", Price = 79.99m, CategoryId = 3, ImageUrl = "https://placehold.co/300x300/ffc107/000000?text=Coffee+Maker", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 10, Name = "Indoor Plant Set", Description = "Set of 3 low-maintenance indoor plants", Price = 34.99m, CategoryId = 3, ImageUrl = "https://placehold.co/300x300/ffc107/000000?text=Plants", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 11, Name = "LED Desk Lamp", Description = "Adjustable LED desk lamp with multiple brightness levels", Price = 44.99m, CategoryId = 3, ImageUrl = "https://placehold.co/300x300/ffc107/000000?text=Desk+Lamp", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 12, Name = "Storage Organizer", Description = "Multi-compartment storage organizer for home office", Price = 29.99m, CategoryId = 3, ImageUrl = "https://placehold.co/300x300/ffc107/000000?text=Organizer", IsActive = true, CreatedAt = DateTime.UtcNow },
            // Sports & Outdoors
            new() { Id = 13, Name = "Yoga Mat", Description = "Non-slip yoga mat with carrying strap", Price = 24.99m, CategoryId = 4, ImageUrl = "https://placehold.co/300x300/dc3545/ffffff?text=Yoga+Mat", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 14, Name = "Water Bottle", Description = "Insulated stainless steel water bottle", Price = 14.99m, CategoryId = 4, ImageUrl = "https://placehold.co/300x300/dc3545/ffffff?text=Water+Bottle", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 15, Name = "Camping Tent", Description = "2-person waterproof camping tent", Price = 199.99m, CategoryId = 4, ImageUrl = "https://placehold.co/300x300/dc3545/ffffff?text=Tent", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 16, Name = "Bicycle Helmet", Description = "Lightweight bicycle helmet with adjustable fit", Price = 49.99m, CategoryId = 4, ImageUrl = "https://placehold.co/300x300/dc3545/ffffff?text=Helmet", IsActive = true, CreatedAt = DateTime.UtcNow },
            // Books
            new() { Id = 17, Name = "Fiction Novel", Description = "Bestselling fiction novel - The Great Adventure", Price = 14.99m, CategoryId = 5, ImageUrl = "https://placehold.co/300x300/6c757d/ffffff?text=Novel", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 18, Name = "Programming Guide", Description = "Comprehensive guide to modern programming", Price = 39.99m, CategoryId = 5, ImageUrl = "https://placehold.co/300x300/6c757d/ffffff?text=Programming", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 19, Name = "Cookbook", Description = "Collection of 100 delicious recipes", Price = 24.99m, CategoryId = 5, ImageUrl = "https://placehold.co/300x300/6c757d/ffffff?text=Cookbook", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 20, Name = "Self-Help Book", Description = "Guide to personal development and success", Price = 19.99m, CategoryId = 5, ImageUrl = "https://placehold.co/300x300/6c757d/ffffff?text=Self-Help", IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}

// API Endpoints
var api = app.MapGroup("/api");

// Categories
api.MapGet("/categories", async (ProductCatalogService service) =>
    Results.Ok(await service.GetCategoriesAsync()))
    .WithName("GetCategories")
    .WithOpenApi();

api.MapGet("/categories/{id:int}", async (int id, ProductCatalogService service) =>
{
    var category = await service.GetCategoryByIdAsync(id);
    return category is null ? Results.NotFound() : Results.Ok(category);
})
.WithName("GetCategory")
.WithOpenApi();

// Products
api.MapGet("/products", async ([AsParameters] ProductsRequest request, ProductCatalogService service) =>
    Results.Ok(await service.GetProductsAsync(request)))
    .WithName("GetProducts")
    .WithOpenApi();

api.MapGet("/products/featured", async (ProductCatalogService service) =>
    Results.Ok(await service.GetFeaturedProductsAsync()))
    .WithName("GetFeaturedProducts")
    .WithOpenApi();

api.MapGet("/products/{id:int}", async (int id, ProductCatalogService service) =>
{
    var product = await service.GetProductByIdAsync(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
})
.WithName("GetProduct")
.WithOpenApi();

app.Run();
