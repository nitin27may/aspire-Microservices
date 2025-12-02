using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerce.UserManagement.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.UserManagement.Api.Services;

/// <summary>
/// Service for handling user authentication and management.
/// </summary>
public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return (false, "A user with this email already exists.", null);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ShippingAddress = request.ShippingAddress,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("User registration failed: {Errors}", errors);
            return (false, errors, null);
        }

        _logger.LogInformation("User registered: {Email}", request.Email);
        return (true, null, GenerateAuthResponse(user));
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, "Invalid email or password.", null);
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            return (false, "Invalid email or password.", null);
        }

        _logger.LogInformation("User logged in: {Email}", request.Email);
        return (true, null, GenerateAuthResponse(user));
    }

    /// <summary>
    /// Gets user profile by ID.
    /// </summary>
    public async Task<UserProfileDto?> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        return new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            ShippingAddress = user.ShippingAddress,
            City = user.City,
            PostalCode = user.PostalCode,
            Country = user.Country
        };
    }

    /// <summary>
    /// Gets user profile by email.
    /// </summary>
    public async Task<UserProfileDto?> GetProfileByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return null;

        return new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            ShippingAddress = user.ShippingAddress,
            City = user.City,
            PostalCode = user.PostalCode,
            Country = user.Country
        };
    }

    /// <summary>
    /// Updates user profile.
    /// </summary>
    public async Task<(bool Success, string? Error)> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.ShippingAddress = request.ShippingAddress;
        user.City = request.City;
        user.PostalCode = request.PostalCode;
        user.Country = request.Country;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded 
            ? (true, null) 
            : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    private AuthResponse GenerateAuthResponse(ApplicationUser user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var token = GenerateJwtToken(user, expiresAt);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = expiresAt
        };
    }

    private string GenerateJwtToken(ApplicationUser user, DateTime expiresAt)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "SuperSecretKeyForDevelopment12345678901234567890";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ECommerce",
            audience: _configuration["Jwt:Audience"] ?? "ECommerce",
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
