using Microsoft.EntityFrameworkCore;
using StockReplenishment.Models;

namespace StockReplenishment.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<ReplenishmentRequest> ReplenishmentRequests => Set<ReplenishmentRequest>();
    public DbSet<ReplenishmentRequestItem> ReplenishmentRequestItems => Set<ReplenishmentRequestItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<ReplenishmentRequest>()
            .HasOne(x => x.Location)
            .WithMany(x => x.Requests)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReplenishmentRequestItem>()
            .HasOne(x => x.ReplenishmentRequest)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ReplenishmentRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
