using Microsoft.EntityFrameworkCore;
using StockReplenishment.Models;

namespace StockReplenishment.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Locations.AnyAsync())
            return;

        var locations = new List<Location>
        {
            new() { Id = 1, Code = "LOC-001", Name = "Assembly Line 1" },
            new() { Id = 2, Code = "LOC-002", Name = "Assembly Line 2" },
            new() { Id = 3, Code = "WH-001", Name = "Main Warehouse" }
        };

        db.Locations.AddRange(locations);

        db.ReplenishmentRequests.AddRange(
            new ReplenishmentRequest
            {
                Id = 1,
                LocationId = 1,
                Priority = RequestPriority.Urgent,
                Status = RequestStatus.Submitted,
                StockValidationStatus = StockValidationStatus.Passed,
                StockValidationMessage = "Stock is available for all requested items.",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-5),
                SubmittedAtUtc = DateTime.UtcNow.AddHours(-4),
                Items = new List<ReplenishmentRequestItem>
                {
                    new() { ArticleNumber = "MAT-1001", Description = "Bearing", RequestedQuantity = 20 }
                }
            },
            new ReplenishmentRequest
            {
                Id = 2,
                LocationId = 2,
                Priority = RequestPriority.Normal,
                Status = RequestStatus.Draft,
                StockValidationStatus = StockValidationStatus.NotStarted,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                Items = new List<ReplenishmentRequestItem>
                {
                    new() { ArticleNumber = "MAT-1002", Description = "Motor Coupling", RequestedQuantity = 10 }
                }
            },
            new ReplenishmentRequest
            {
                Id = 3,
                LocationId = 3,
                Priority = RequestPriority.Low,
                Status = RequestStatus.Fulfilled,
                StockValidationStatus = StockValidationStatus.Passed,
                StockValidationMessage = "Stock is available for all requested items.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                SubmittedAtUtc = DateTime.UtcNow.AddHours(-20),
                ApprovedAtUtc = DateTime.UtcNow.AddHours(-18),
                FulfilledAtUtc = DateTime.UtcNow.AddHours(-16),
                Items = new List<ReplenishmentRequestItem>
                {
                    new() { ArticleNumber = "MAT-1003", Description = "Fastener Kit", RequestedQuantity = 50, FulfilledQuantity = 50 }
                }
            }
        );

        await db.SaveChangesAsync();
    }
}
