using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StockReplenishment.Data;
using StockReplenishment.DTOs;
using StockReplenishment.Models;
using StockReplenishment.Services;

namespace StockReplenishment.Tests;

[TestFixture]
public class ReplenishmentServiceTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private async Task<Location> AddLocationAsync(
        AppDbContext db,
        int id = 1)
    {
        var location = new Location
        {
            Id = id,
            Code = $"LOC{id}",
            Name = $"Location {id}"
        };

        db.Locations.Add(location);
        await db.SaveChangesAsync();

        return location;
    }

    private async Task<ReplenishmentRequest> AddRequestAsync(
        AppDbContext db,
        RequestStatus status = RequestStatus.Draft,
        StockValidationStatus stockValidationStatus =
            StockValidationStatus.NotStarted)
    {
        var location = await AddLocationAsync(db);

        var request = new ReplenishmentRequest
        {
            LocationId = location.Id,
            Location = location,
            Priority = RequestPriority.Normal,
            Status = status,
            StockValidationStatus = stockValidationStatus,
            CreatedAtUtc = DateTime.UtcNow,

            Items = new List<ReplenishmentRequestItem>
            {
                new()
                {
                    ArticleNumber = "ART001",
                    Description = "Test Material",
                    RequestedQuantity = 10,
                    FulfilledQuantity = 0
                }
            }
        };

        db.ReplenishmentRequests.Add(request);
        await db.SaveChangesAsync();

        return request;
    }

    // =========================================================
    // CREATE
    // =========================================================

    [Test]
    public async Task CreateAsync_WithValidRequest_ShouldCreateDraftRequest()
    {
        // Arrange
        await using var db = CreateDbContext();

        await AddLocationAsync(db);

        var service = new ReplenishmentService(db);

        var dto = new CreateRequestDto
        {
            LocationId = 1,
            Priority = RequestPriority.Normal,
            Items = new List<CreateRequestItemDto>
            {
                new()
                {
                    ArticleNumber = " ART001 ",
                    Description = " Test Material ",
                    RequestedQuantity = 10
                }
            }
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.GreaterThan(0));

        Assert.That(
            result.LocationId,
            Is.EqualTo(1));

        Assert.That(
            result.Status,
            Is.EqualTo(RequestStatus.Draft));

        Assert.That(
            result.StockValidationStatus,
            Is.EqualTo(StockValidationStatus.NotStarted));

        Assert.That(
            result.Items,
            Has.Count.EqualTo(1));

        Assert.That(
            result.Items[0].ArticleNumber,
            Is.EqualTo("ART001"));

        Assert.That(
            result.Items[0].Description,
            Is.EqualTo("Test Material"));

        Assert.That(
            result.Items[0].RequestedQuantity,
            Is.EqualTo(10));

        Assert.That(
            result.Items[0].FulfilledQuantity,
            Is.EqualTo(0));
    }

    [Test]
    public async Task CreateAsync_WithInvalidLocation_ShouldThrowException()
    {
        // Arrange
        await using var db = CreateDbContext();

        var service = new ReplenishmentService(db);

        var dto = new CreateRequestDto
        {
            LocationId = 999,
            Priority = RequestPriority.Normal,
            Items = new List<CreateRequestItemDto>
            {
                new()
                {
                    ArticleNumber = "ART001",
                    Description = "Test Material",
                    RequestedQuantity = 10
                }
            }
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateAsync(dto));

        Assert.That(
            exception!.Message,
            Is.EqualTo("The selected location does not exist."));
    }

    [Test]
    public async Task CreateAsync_WithNoItems_ShouldThrowException()
    {
        // Arrange
        await using var db = CreateDbContext();

        await AddLocationAsync(db);

        var service = new ReplenishmentService(db);

        var dto = new CreateRequestDto
        {
            LocationId = 1,
            Priority = RequestPriority.Normal,
            Items = new List<CreateRequestItemDto>()
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateAsync(dto));

        Assert.That(
            exception!.Message,
            Is.EqualTo("At least one material item is required."));
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Test]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnRequest()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(db);

        var service = new ReplenishmentService(db);

        // Act
        var result = await service.GetByIdAsync(request.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(request.Id));

        Assert.That(
            result.Status,
            Is.EqualTo(RequestStatus.Draft));

        Assert.That(
            result.Items,
            Has.Count.EqualTo(1));

        Assert.That(
            result.Items[0].ArticleNumber,
            Is.EqualTo("ART001"));
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        await using var db = CreateDbContext();

        var service = new ReplenishmentService(db);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        Assert.That(result, Is.Null);
    }

    // =========================================================
    // GET REQUESTS / FILTERING / PAGINATION
    // =========================================================

    [Test]
    public async Task GetRequestsAsync_ShouldReturnRequests()
    {
        // Arrange
        await using var db = CreateDbContext();

        await AddRequestAsync(db);

        var service = new ReplenishmentService(db);

        // Act
        var result = await service.GetRequestsAsync(
            null,
            null,
            null,
            1,
            10);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Page, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(10));
    }

    [Test]
    public async Task GetRequestsAsync_WithStatusFilter_ShouldReturnMatchingRequests()
    {
        // Arrange
        await using var db = CreateDbContext();

        await AddRequestAsync(
            db,
            RequestStatus.Draft);

        var submitted = await AddRequestAsync(
            db,
            RequestStatus.Submitted,
            StockValidationStatus.InProgress);

        var service = new ReplenishmentService(db);

        // Act
        var result = await service.GetRequestsAsync(
            RequestStatus.Submitted,
            null,
            null,
            1,
            10);

        // Assert
        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(
            result.Items[0].Status,
            Is.EqualTo(RequestStatus.Submitted));
    }

    [Test]
    public async Task GetRequestsAsync_WithPriorityFilter_ShouldReturnMatchingRequests()
    {
        // Arrange
        await using var db = CreateDbContext();

        var location = await AddLocationAsync(db);

        db.ReplenishmentRequests.AddRange(
            new ReplenishmentRequest
            {
                LocationId = location.Id,
                Priority = RequestPriority.Urgent,
                Status = RequestStatus.Draft,
                StockValidationStatus = StockValidationStatus.NotStarted,
                CreatedAtUtc = DateTime.UtcNow
            },
            new ReplenishmentRequest
            {
                LocationId = location.Id,
                Priority = RequestPriority.Low,
                Status = RequestStatus.Draft,
                StockValidationStatus = StockValidationStatus.NotStarted,
                CreatedAtUtc = DateTime.UtcNow
            });

        await db.SaveChangesAsync();

        var service = new ReplenishmentService(db);

        // Act
        var result = await service.GetRequestsAsync(
            null,
            RequestPriority.Urgent,
            null,
            1,
            10);

        // Assert
        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(
            result.Items[0].Priority,
            Is.EqualTo(RequestPriority.Urgent));
    }

    [Test]
    public async Task GetRequestsAsync_WithLocationFilter_ShouldReturnMatchingRequests()
    {
        // Arrange
        await using var db = CreateDbContext();

        await AddLocationAsync(db, 1);
        await AddLocationAsync(db, 2);

        db.ReplenishmentRequests.AddRange(
            new ReplenishmentRequest
            {
                LocationId = 1,
                Priority = RequestPriority.Normal,
                Status = RequestStatus.Draft,
                StockValidationStatus = StockValidationStatus.NotStarted,
                CreatedAtUtc = DateTime.UtcNow
            },
            new ReplenishmentRequest
            {
                LocationId = 2,
                Priority = RequestPriority.Normal,
                Status = RequestStatus.Draft,
                StockValidationStatus = StockValidationStatus.NotStarted,
                CreatedAtUtc = DateTime.UtcNow
            });

        await db.SaveChangesAsync();

        var service = new ReplenishmentService(db);

        // Act
        var result = await service.GetRequestsAsync(
            null,
            null,
            2,
            1,
            10);

        // Assert
        Assert.That(result.TotalCount, Is.EqualTo(1));

        Assert.That(
            result.Items[0].LocationId,
            Is.EqualTo(2));
    }

    [Test]
    public async Task GetRequestsAsync_WithInvalidPage_ShouldNormalizePageToOne()
    {
        // Arrange
        await using var db = CreateDbContext();

        await AddRequestAsync(db);

        var service = new ReplenishmentService(db);

        // Act
        var result = await service.GetRequestsAsync(
            null,
            null,
            null,
            0,
            10);

        // Assert
        Assert.That(result.Page, Is.EqualTo(1));
    }

    // =========================================================
    // SUBMIT
    // =========================================================

    [Test]
    public async Task SubmitAsync_WithDraftRequest_ShouldSubmitRequest()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Draft,
            StockValidationStatus.NotStarted);

        var service = new ReplenishmentService(db);

        // Act
        var result = await service.SubmitAsync(request.Id);

        // Assert
        Assert.That(
            result.Status,
            Is.EqualTo(RequestStatus.Submitted));

        Assert.That(
            result.StockValidationStatus,
            Is.EqualTo(StockValidationStatus.InProgress));

        Assert.That(
            result.StockValidationMessage,
            Is.EqualTo("Stock validation is in progress."));

        Assert.That(
            result.SubmittedAtUtc,
            Is.Not.Null);
    }

    [Test]
    public async Task SubmitAsync_WithNonDraftRequest_ShouldThrowException()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Submitted,
            StockValidationStatus.InProgress);

        var service = new ReplenishmentService(db);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.SubmitAsync(request.Id));

        Assert.That(
            exception!.Message,
            Is.EqualTo("Only draft requests can be submitted."));
    }

    [Test]
    public async Task SubmitAsync_WithNonExistingRequest_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        await using var db = CreateDbContext();

        var service = new ReplenishmentService(db);

        // Act & Assert
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.SubmitAsync(999));

        Assert.That(
            exception!.Message,
            Is.EqualTo("Request not found."));
    }

    // =========================================================
    // APPROVE
    // =========================================================

    [Test]
    public async Task ApproveAsync_WithValidatedSubmittedRequest_ShouldApproveRequest()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Submitted,
            StockValidationStatus.Passed);

        var service = new ReplenishmentService(db);

        // Act
        var result = await service.ApproveAsync(request.Id);

        // Assert
        Assert.That(
            result.Status,
            Is.EqualTo(RequestStatus.Approved));

        Assert.That(
            result.ApprovedAtUtc,
            Is.Not.Null);
    }

    [Test]
    public async Task ApproveAsync_WithoutPassedStockValidation_ShouldThrowException()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Submitted,
            StockValidationStatus.Failed);

        var service = new ReplenishmentService(db);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ApproveAsync(request.Id));

        Assert.That(
            exception!.Message,
            Is.EqualTo(
                "The request can only be approved after stock validation passes."));
    }

    // =========================================================
    // REJECT
    // =========================================================

    [Test]
    public async Task RejectAsync_WithValidReason_ShouldRejectRequest()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Submitted,
            StockValidationStatus.Failed);

        var service = new ReplenishmentService(db);

        var dto = new RejectRequestDto
        {
            Reason = " Insufficient stock "
        };

        // Act
        var result = await service.RejectAsync(
            request.Id,
            dto);

        // Assert
        Assert.That(
            result.Status,
            Is.EqualTo(RequestStatus.Rejected));

        Assert.That(
            result.RejectionReason,
            Is.EqualTo("Insufficient stock"));
    }

    [Test]
    public async Task RejectAsync_WithEmptyReason_ShouldThrowException()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Submitted,
            StockValidationStatus.Failed);

        var service = new ReplenishmentService(db);

        var dto = new RejectRequestDto
        {
            Reason = " "
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.RejectAsync(
                request.Id,
                dto));

        Assert.That(
            exception!.Message,
            Is.EqualTo("A rejection reason is required."));
    }

    // =========================================================
    // FULFILL
    // =========================================================

    [Test]
    public async Task FulfillAsync_WithApprovedRequestAndFullQuantity_ShouldFulfillRequest()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Approved,
            StockValidationStatus.Passed);

        var itemId = request.Items.First().Id;

        var service = new ReplenishmentService(db);

        var dto = new FulfillRequestDto
        {
            Items = new List<FulfillItemDto>
            {
                new()
                {
                    ItemId = itemId,
                    FulfilledQuantity = 10
                }
            }
        };

        // Act
        var result = await service.FulfillAsync(
            request.Id,
            dto);

        // Assert
        Assert.That(
            result.Status,
            Is.EqualTo(RequestStatus.Fulfilled));

        Assert.That(
            result.FulfilledAtUtc,
            Is.Not.Null);

        Assert.That(
            result.Items[0].FulfilledQuantity,
            Is.EqualTo(10));
    }

    [Test]
    public async Task FulfillAsync_WithPartialQuantity_ShouldThrowException()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Approved,
            StockValidationStatus.Passed);

        var itemId = request.Items.First().Id;

        var service = new ReplenishmentService(db);

        var dto = new FulfillRequestDto
        {
            Items = new List<FulfillItemDto>
            {
                new()
                {
                    ItemId = itemId,
                    FulfilledQuantity = 5
                }
            }
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.FulfillAsync(
                request.Id,
                dto));

        Assert.That(
            exception!.Message,
            Is.EqualTo(
                "All requested quantities must be fulfilled before completing the request."));
    }

    [Test]
    public async Task FulfillAsync_WhenRequestIsNotApproved_ShouldThrowException()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Submitted,
            StockValidationStatus.Passed);

        var service = new ReplenishmentService(db);

        var dto = new FulfillRequestDto
        {
            Items = new List<FulfillItemDto>()
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.FulfillAsync(
                request.Id,
                dto));

        Assert.That(
            exception!.Message,
            Is.EqualTo("Only approved requests can be fulfilled."));
    }

    // =========================================================
    // STOCK VALIDATION
    // =========================================================

    [Test]
    public async Task ApplyStockValidationResultAsync_WhenAvailable_ShouldSetPassed()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Submitted,
            StockValidationStatus.InProgress);

        var service = new ReplenishmentService(db);

        // Act
        await service.ApplyStockValidationResultAsync(
            request.Id,
            true,
            "Stock available");

        // Assert
        var updated = await db.ReplenishmentRequests
            .FirstAsync(x => x.Id == request.Id);

        Assert.That(
            updated.StockValidationStatus,
            Is.EqualTo(StockValidationStatus.Passed));

        Assert.That(
            updated.StockValidationMessage,
            Is.EqualTo("Stock available"));
    }

    [Test]
    public async Task ApplyStockValidationResultAsync_WhenUnavailable_ShouldSetFailed()
    {
        // Arrange
        await using var db = CreateDbContext();

        var request = await AddRequestAsync(
            db,
            RequestStatus.Submitted,
            StockValidationStatus.InProgress);

        var service = new ReplenishmentService(db);

        // Act
        await service.ApplyStockValidationResultAsync(
            request.Id,
            false,
            "Insufficient stock");

        // Assert
        var updated = await db.ReplenishmentRequests
            .FirstAsync(x => x.Id == request.Id);

        Assert.That(
            updated.StockValidationStatus,
            Is.EqualTo(StockValidationStatus.Failed));

        Assert.That(
            updated.StockValidationMessage,
            Is.EqualTo("Insufficient stock"));
    }
}