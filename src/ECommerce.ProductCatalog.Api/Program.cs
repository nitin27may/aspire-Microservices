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
}

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await context.Database.EnsureCreatedAsync();
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
