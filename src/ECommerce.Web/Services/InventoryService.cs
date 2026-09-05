using ECommerce.Web.Models;

namespace ECommerce.Web.Services;

/// <summary>
/// Service for communicating with the Inventory API.
/// </summary>
public class InventoryService
{
    private readonly HttpClient _httpClient;

    public InventoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InventoryDto?> GetInventoryAsync(int productId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<InventoryDto>($"/api/inventory/{productId}");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks stock availability for multiple items.
    /// </summary>
    public async Task<StockCheckResponse?> CheckStockAsync(List<StockCheckRequestItem> items)
    {
        try
        {
            var request = new StockCheckRequest { Items = items };
            var response = await _httpClient.PostAsJsonAsync("/api/inventory/check", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StockCheckResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
