using ECommerce.Notifications.Api.BackgroundServices;
using ECommerce.Notifications.Api.Data;
using ECommerce.Notifications.Api.Models;
using ECommerce.Notifications.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<NotificationsDbContext>("notificationsdb");

builder.AddRabbitMQClient("rabbitmq");

builder.Services.AddScoped<EmailService>();
builder.Services.AddHostedService<NotificationConsumer>();

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
    var context = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// API Endpoints
var api = app.MapGroup("/api");

api.MapGet("/emails", async (int page, int pageSize, NotificationsDbContext db) =>
{
    var actualPage = page > 0 ? page : 1;
    var actualPageSize = pageSize > 0 ? pageSize : 10;

    var query = db.EmailLogs.OrderByDescending(e => e.CreatedAt);
    var totalCount = await query.CountAsync();

    var items = await query
        .Skip((actualPage - 1) * actualPageSize)
        .Take(actualPageSize)
        .Select(e => new EmailLogDto
        {
            Id = e.Id,
            Recipient = e.Recipient,
            Subject = e.Subject,
            BodyPreview = e.BodyPreview,
            Status = e.Status.ToString(),
            SentAt = e.SentAt,
            ErrorMessage = e.ErrorMessage,
            CreatedAt = e.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(new PaginatedEmailLogsResponse
    {
        Items = items,
        TotalCount = totalCount,
        Page = actualPage,
        PageSize = actualPageSize
    });
})
.WithName("GetEmailLogs")
.WithOpenApi();

api.MapGet("/emails/{id:int}", async (int id, NotificationsDbContext db) =>
{
    var email = await db.EmailLogs.FindAsync(id);
    if (email == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new EmailLogDto
    {
        Id = email.Id,
        Recipient = email.Recipient,
        Subject = email.Subject,
        BodyPreview = email.BodyPreview,
        Status = email.Status.ToString(),
        SentAt = email.SentAt,
        ErrorMessage = email.ErrorMessage,
        CreatedAt = email.CreatedAt
    });
})
.WithName("GetEmailLog")
.WithOpenApi();

app.Run();
