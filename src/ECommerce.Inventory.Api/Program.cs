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

    // Redirect root to Scalar API docs
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await context.Database.EnsureCreatedAsync();

    // Seed inventory data if empty
    if (!context.Inventory.Any())
    {
        var inventoryItems = Enumerable.Range(1, 20)
            .Select(id => new InventoryItem
            {
                ProductId = id,
                Stock = 100,
                ReservedStock = 0,
                LastUpdated = DateTime.UtcNow
            })
            .ToList();

        context.Inventory.AddRange(inventoryItems);
        await context.SaveChangesAsync();
    }
}

// API Endpoints
var api = app.MapGroup("/api");

api.MapGet("/inventory/{productId:int}", async (int productId, InventoryService service) =>
{
    var inventory = await service.GetInventoryAsync(productId);
    return inventory is null ? Results.NotFound() : Results.Ok(inventory);
})
.WithName("GetInventory");

api.MapPost("/inventory/check", async (StockCheckRequest request, InventoryService service) =>
    Results.Ok(await service.CheckStockAsync(request)))
    .WithName("CheckStock");

api.MapPost("/inventory/reserve", async (ReserveInventoryRequest request, InventoryService service) =>
{
    var result = await service.ReserveInventoryAsync(request);
    return result.Success
        ? Results.Ok(result)
        : Results.Conflict(result);
})
.WithName("ReserveInventory");

app.Run();
