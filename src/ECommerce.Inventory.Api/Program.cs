using ECommerce.Inventory.Api.BackgroundServices;
using ECommerce.Inventory.Api.Data;
using ECommerce.Inventory.Api.Models;
using ECommerce.Inventory.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<InventoryDbContext>("inventorydb");

builder.AddRedisClient("redis");

builder.AddRabbitMQClient("rabbitmq");

builder.Services.AddScoped<InventoryService>();
builder.Services.AddHostedService<OrderEventConsumer>();

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
    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// API Endpoints
var api = app.MapGroup("/api");

api.MapGet("/inventory/{productId:int}", async (int productId, InventoryService service) =>
{
    var inventory = await service.GetInventoryAsync(productId);
    return inventory is null ? Results.NotFound() : Results.Ok(inventory);
})
.WithName("GetInventory")
.WithOpenApi();

api.MapPost("/inventory/check", async (StockCheckRequest request, InventoryService service) =>
    Results.Ok(await service.CheckStockAsync(request)))
    .WithName("CheckStock")
    .WithOpenApi();

api.MapPost("/inventory/reserve", async (ReserveInventoryRequest request, InventoryService service) =>
{
    var result = await service.ReserveInventoryAsync(request);
    return result.Success 
        ? Results.Ok(result) 
        : Results.Conflict(result);
})
.WithName("ReserveInventory")
.WithOpenApi();

app.Run();
