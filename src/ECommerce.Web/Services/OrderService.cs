using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using ECommerce.Web.Models;

namespace ECommerce.Web.Services;

/// <summary>
/// Service for communicating with the Order API.
/// </summary>
public class OrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public OrderService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<(bool Success, string? Error, OrderDto? Order)> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                return (false, "Please log in to place an order.", null);
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.PostAsJsonAsync("/api/orders", request);

            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderDto>();
                return (true, null, order);
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Error ?? "Failed to create order", null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<PaginatedOrdersResponse> GetOrdersAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                return new PaginatedOrdersResponse();
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetFromJsonAsync<PaginatedOrdersResponse>($"/api/orders?page={page}&pageSize={pageSize}");
            return response ?? new PaginatedOrdersResponse();
        }
        catch
        {
            return new PaginatedOrdersResponse();
        }
    }

    public async Task<OrderDto?> GetOrderAsync(string orderNumber)
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _httpClient.GetFromJsonAsync<OrderDto>($"/api/orders/{orderNumber}");
        }
        catch
        {
            return null;
        }
    }
}
