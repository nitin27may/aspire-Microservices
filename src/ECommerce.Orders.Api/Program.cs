using System.Security.Claims;
using System.Text;
using ECommerce.Orders.Api.Data;
using ECommerce.Orders.Api.Models;
using ECommerce.Orders.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<OrdersDbContext>("ordersdb");

builder.AddRabbitMQClient("rabbitmq");

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForDevelopment12345678901234567890";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ECommerce";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ECommerce";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("inventoryapi", client =>
{
    client.BaseAddress = new Uri("https+http://inventoryapi");
});

builder.Services.AddHttpClient("usermanagementapi", client =>
{
    client.BaseAddress = new Uri("https+http://usermanagementapi");
});

builder.Services.AddScoped<OrderService>();

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
    var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

// API Endpoints
var api = app.MapGroup("/api");

api.MapPost("/orders", async (CreateOrderRequest request, OrderService service, HttpContext context) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var (success, error, order) = await service.CreateOrderAsync(userId, request);
    return success 
        ? Results.Created($"/api/orders/{order!.OrderNumber}", order) 
        : Results.Conflict(new { error });
})
.RequireAuthorization()
.WithName("CreateOrder")
.WithOpenApi();

api.MapGet("/orders", async (int page, int pageSize, OrderService service, HttpContext context) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var orders = await service.GetOrdersByUserAsync(userId, page > 0 ? page : 1, pageSize > 0 ? pageSize : 10);
    return Results.Ok(orders);
})
.RequireAuthorization()
.WithName("GetOrders")
.WithOpenApi();

api.MapGet("/orders/{orderNumber}", async (string orderNumber, OrderService service, HttpContext context) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var order = await service.GetOrderByNumberAsync(orderNumber);
    if (order == null)
    {
        return Results.NotFound();
    }

    // Only allow users to view their own orders
    if (order.UserId != userId)
    {
        return Results.Forbid();
    }

    return Results.Ok(order);
})
.RequireAuthorization()
.WithName("GetOrder")
.WithOpenApi();

app.Run();
