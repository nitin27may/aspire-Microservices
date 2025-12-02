using Blazored.LocalStorage;
using ECommerce.Web.Models;

namespace ECommerce.Web.Services;

/// <summary>
/// Service for managing the shopping cart.
/// </summary>
public class CartService
{
    private readonly ILocalStorageService _localStorage;
    private const string CartKey = "shopping_cart";

    public event Action? OnCartChanged;

    public CartService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<ShoppingCart> GetCartAsync()
    {
        var cart = await _localStorage.GetItemAsync<ShoppingCart>(CartKey);
        return cart ?? new ShoppingCart();
    }

    public async Task AddToCartAsync(ProductDto product, int quantity = 1)
    {
        var cart = await GetCartAsync();
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == product.Id);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                Quantity = quantity
            });
        }

        await SaveCartAsync(cart);
    }

    public async Task UpdateQuantityAsync(int productId, int quantity)
    {
        var cart = await GetCartAsync();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item != null)
        {
            if (quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
        }

        await SaveCartAsync(cart);
    }

    public async Task RemoveFromCartAsync(int productId)
    {
        var cart = await GetCartAsync();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item != null)
        {
            cart.Items.Remove(item);
            await SaveCartAsync(cart);
        }
    }

    public async Task ClearCartAsync()
    {
        await _localStorage.RemoveItemAsync(CartKey);
        OnCartChanged?.Invoke();
    }

    public async Task<int> GetCartCountAsync()
    {
        var cart = await GetCartAsync();
        return cart.ItemCount;
    }

    private async Task SaveCartAsync(ShoppingCart cart)
    {
        await _localStorage.SetItemAsync(CartKey, cart);
        OnCartChanged?.Invoke();
    }
}
