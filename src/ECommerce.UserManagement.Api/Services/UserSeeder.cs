using ECommerce.UserManagement.Api.Data;
using ECommerce.UserManagement.Api.Models;

namespace ECommerce.UserManagement.Api.Services;

/// <summary>
/// Service for seeding initial users.
/// </summary>
public class UserSeeder
{
    private readonly UsersDbContext _dbContext;
    private readonly AuthService _authService;
    private readonly ILogger<UserSeeder> _logger;

    public UserSeeder(
        UsersDbContext dbContext,
        AuthService authService,
        ILogger<UserSeeder> logger)
    {
        _dbContext = dbContext;
        _authService = authService;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // Demo User
        var demoUserExists = await _authService.GetProfileByEmailAsync("demo@example.com");
        if (demoUserExists == null)
        {
            var result = await _authService.RegisterAsync(new RegisterRequest
            {
                Email = "demo@example.com",
                Password = "Demo123!",
                FirstName = "John",
                LastName = "Doe",
                ShippingAddress = "123 Main St",
                City = "Toronto",
                PostalCode = "M5H 2N2",
                Country = "Canada"
            });

            if (result.Success)
            {
                _logger.LogInformation("Demo user created: demo@example.com");
            }
        }

        // Test User
        var testUserExists = await _authService.GetProfileByEmailAsync("test@example.com");
        if (testUserExists == null)
        {
            var result = await _authService.RegisterAsync(new RegisterRequest
            {
                Email = "test@example.com",
                Password = "Test123!",
                FirstName = "Jane",
                LastName = "Smith",
                ShippingAddress = "456 Oak Ave",
                City = "Vancouver",
                PostalCode = "V6B 1A1",
                Country = "Canada"
            });

            if (result.Success)
            {
                _logger.LogInformation("Test user created: test@example.com");
            }
        }
    }
}
