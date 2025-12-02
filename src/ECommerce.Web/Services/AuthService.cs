using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using ECommerce.Web.Models;

namespace ECommerce.Web.Services;

/// <summary>
/// Service for handling authentication.
/// </summary>
public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "auth_token";
    private const string UserKey = "current_user";

    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null)
                {
                    await _localStorage.SetItemAsync(TokenKey, authResponse.Token);
                    await _localStorage.SetItemAsync(UserKey, authResponse);
                    OnAuthStateChanged?.Invoke();
                    return (true, null);
                }
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Error ?? "Login failed");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null)
                {
                    await _localStorage.SetItemAsync(TokenKey, authResponse.Token);
                    await _localStorage.SetItemAsync(UserKey, authResponse);
                    OnAuthStateChanged?.Invoke();
                    return (true, null);
                }
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Error ?? "Registration failed");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(UserKey);
        OnAuthStateChanged?.Invoke();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(TokenKey);
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TokenKey);
    }

    public async Task<AuthResponse?> GetCurrentUserAsync()
    {
        return await _localStorage.GetItemAsync<AuthResponse>(UserKey);
    }
}

public record ErrorResponse
{
    public string? Error { get; init; }
}
