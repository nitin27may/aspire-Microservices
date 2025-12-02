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
}
