using System.Text;
using ECommerce.UserManagement.Api.Data;
using ECommerce.UserManagement.Api.Models;
using ECommerce.UserManagement.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<UsersDbContext>("usersdb");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<UsersDbContext>()
.AddDefaultTokenProviders();

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

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserSeeder>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Initialize database and seed users
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    var seeder = scope.ServiceProvider.GetRequiredService<UserSeeder>();
    await seeder.SeedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

// API Endpoints
var api = app.MapGroup("/api");

api.MapPost("/auth/register", async (RegisterRequest request, AuthService service) =>
{
    var (success, error, response) = await service.RegisterAsync(request);
    return success 
        ? Results.Ok(response) 
        : Results.BadRequest(new { error });
})
.WithName("Register")
.WithOpenApi();

api.MapPost("/auth/login", async (LoginRequest request, AuthService service) =>
{
    var (success, error, response) = await service.LoginAsync(request);
    return success 
        ? Results.Ok(response) 
        : Results.BadRequest(new { error });
})
.WithName("Login")
.WithOpenApi();

api.MapGet("/users/{userId}", async (string userId, AuthService service) =>
{
    var profile = await service.GetProfileAsync(userId);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
})
.WithName("GetUserProfile")
.WithOpenApi();

api.MapGet("/users/email/{email}", async (string email, AuthService service) =>
{
    var profile = await service.GetProfileByEmailAsync(email);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
})
.WithName("GetUserByEmail")
.WithOpenApi();

api.MapPut("/users/{userId}", async (string userId, UpdateProfileRequest request, AuthService service) =>
{
    var (success, error) = await service.UpdateProfileAsync(userId, request);
    return success 
        ? Results.Ok(new { message = "Profile updated successfully" }) 
        : Results.BadRequest(new { error });
})
.RequireAuthorization()
.WithName("UpdateProfile")
.WithOpenApi();

app.Run();
