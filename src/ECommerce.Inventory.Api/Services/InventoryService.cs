using System.Text.Json;
using ECommerce.Inventory.Api.Data;
using ECommerce.Inventory.Api.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerce.Inventory.Api.Services;

/// <summary>
/// Service for managing inventory operations.
/// </summary>
public class InventoryService
{
    private readonly InventoryDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<InventoryService> _logger;
    private const int CacheTtlMinutes = 5;

    public InventoryService(
        InventoryDbContext dbContext,
        IConnectionMultiplexer redis,
        ILogger<InventoryService> logger)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Gets inventory for a product.
    /// </summary>
    public async Task<InventoryDto?> GetInventoryAsync(int productId)
    {
        var cacheKey = $"inventory:{productId}";
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(cacheKey);

        if (!cached.IsNullOrEmpty)
        {
            _logger.LogInformation("Cache hit for inventory: {ProductId}", productId);
            return JsonSerializer.Deserialize<InventoryDto>(cached.ToString());
        }

        _logger.LogInformation("Cache miss for inventory: {ProductId}", productId);

        var item = await _dbContext.Inventory.FindAsync(productId);
        if (item == null) return null;

        var dto = new InventoryDto
        {
            ProductId = item.ProductId,
            Stock = item.Stock,
            ReservedStock = item.ReservedStock,
            AvailableStock = item.AvailableStock,
            LastUpdated = item.LastUpdated
        };

        await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(dto), TimeSpan.FromMinutes(CacheTtlMinutes));

        return dto;
    }

    /// <summary>
    /// Checks if requested stock is available.
    /// </summary>
    public async Task<StockCheckResponse> CheckStockAsync(StockCheckRequest request)
    {
        var checkItems = new List<StockCheckItem>();
        var allAvailable = true;

        foreach (var item in request.Items)
        {
            var inventory = await _dbContext.Inventory.FindAsync(item.ProductId);
            var availableQuantity = inventory?.AvailableStock ?? 0;
            var isAvailable = availableQuantity >= item.Quantity;

            if (!isAvailable) allAvailable = false;

            checkItems.Add(new StockCheckItem
            {
                ProductId = item.ProductId,
                RequestedQuantity = item.Quantity,
                AvailableQuantity = availableQuantity,
                IsAvailable = isAvailable
            });
        }

        return new StockCheckResponse
        {
            AllAvailable = allAvailable,
            Items = checkItems
        };
    }

    /// <summary>
    /// Reserves inventory for an order.
    /// </summary>
    public async Task<ReservationResponse> ReserveInventoryAsync(ReserveInventoryRequest request)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in request.Items)
                {
                    var inventory = await _dbContext.Inventory.FindAsync(item.ProductId);
                    if (inventory == null)
                    {
                        return new ReservationResponse
                        {
                            Success = false,
                            Error = $"Product {item.ProductId} not found in inventory."
                        };
                    }

                    if (inventory.AvailableStock < item.Quantity)
                    {
                        return new ReservationResponse
                        {
                            Success = false,
                            Error = $"Insufficient stock for product {item.ProductId}. Available: {inventory.AvailableStock}, Requested: {item.Quantity}"
                        };
                    }

                    inventory.ReservedStock += item.Quantity;
                    inventory.LastUpdated = DateTime.UtcNow;

                    var reservation = new InventoryReservation
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        OrderId = request.OrderId,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                        IsActive = true
                    };

                    _dbContext.Reservations.Add(reservation);

                    // Invalidate cache
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync($"inventory:{item.ProductId}");
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Inventory reserved for order: {OrderId}", request.OrderId);

                return new ReservationResponse { Success = true };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to reserve inventory: {Message}", ex.Message);
                return new ReservationResponse
                {
                    Success = false,
                    Error = $"An error occurred while reserving inventory: {ex.Message}"
                };
            }
        });
    }

    /// <summary>
    /// Deducts inventory after order confirmation.
    /// </summary>
    public async Task<bool> DeductInventoryAsync(int orderId, List<ReserveItem> items)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in items)
                {
                    var inventory = await _dbContext.Inventory.FindAsync(item.ProductId);
                    if (inventory == null) continue;

                    // Find and deactivate reservations
                    var reservations = await _dbContext.Reservations
                        .Where(r => r.OrderId == orderId && r.ProductId == item.ProductId && r.IsActive)
                        .ToListAsync();

                    var totalReserved = reservations.Sum(r => r.Quantity);

                    inventory.Stock -= item.Quantity;
                    inventory.ReservedStock -= totalReserved;
                    inventory.LastUpdated = DateTime.UtcNow;

                    foreach (var reservation in reservations)
                    {
                        reservation.IsActive = false;
                    }

                    // Invalidate cache
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync($"inventory:{item.ProductId}");
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Inventory deducted for order: {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to deduct inventory for order: {OrderId}", orderId);
                return false;
            }
        });
    }

    /// <summary>
    /// Releases expired reservations.
    /// </summary>
    public async Task ReleaseExpiredReservationsAsync()
    {
        var expiredReservations = await _dbContext.Reservations
            .Where(r => r.IsActive && r.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (!expiredReservations.Any()) return;

        var productIds = expiredReservations.Select(r => r.ProductId).Distinct();

        foreach (var productId in productIds)
        {
            var inventory = await _dbContext.Inventory.FindAsync(productId);
            if (inventory == null) continue;

            var reservationsForProduct = expiredReservations.Where(r => r.ProductId == productId);
            var totalToRelease = reservationsForProduct.Sum(r => r.Quantity);

            inventory.ReservedStock -= totalToRelease;
            inventory.LastUpdated = DateTime.UtcNow;

            foreach (var reservation in reservationsForProduct)
            {
                reservation.IsActive = false;
            }

            // Invalidate cache
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync($"inventory:{productId}");
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Released {Count} expired reservations", expiredReservations.Count);
    }
}
