using ECommerce.Inventory.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Api.Data;

/// <summary>
/// Database context for the Inventory service.
/// </summary>
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<InventoryReservation> Reservations => Set<InventoryReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.Stock).HasDefaultValue(0);
            entity.Property(e => e.ReservedStock).HasDefaultValue(0);
        });

        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.ProductId);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed inventory for all 20 products with 100 units each
        var inventoryItems = Enumerable.Range(1, 20)
            .Select(id => new InventoryItem
            {
                ProductId = id,
                Stock = 100,
                ReservedStock = 0,
                LastUpdated = DateTime.UtcNow
            })
            .ToList();

        modelBuilder.Entity<InventoryItem>().HasData(inventoryItems);
    }
}
