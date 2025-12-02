using ECommerce.Notifications.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notifications.Api.Data;

/// <summary>
/// Database context for the Notifications service.
/// </summary>
public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Recipient).HasMaxLength(256);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.BodyPreview).HasMaxLength(1000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        });
    }
}
