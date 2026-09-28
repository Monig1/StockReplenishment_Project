using Microsoft.EntityFrameworkCore;
using StockReplenishment.Data;
using StockReplenishment.DTOs;
using StockReplenishment.Models;

namespace StockReplenishment.Services;

public class ReplenishmentService : IReplenishmentService
{
    private readonly AppDbContext _db;

    public ReplenishmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResultDto<RequestResponseDto>> GetRequestsAsync(
        RequestStatus? status,
        RequestPriority? priority,
        int? locationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ReplenishmentRequests
            .AsNoTracking()
            .Include(x => x.Location)
            .Include(x => x.Items)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(x => x.Priority == priority.Value);

        if (locationId.HasValue)
            query = query.Where(x => x.LocationId == locationId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var requests = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<RequestResponseDto>
        {
            Items = requests.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<RequestResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ReplenishmentRequests
            .AsNoTracking()
            .Include(x => x.Location)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<RequestResponseDto> CreateAsync(CreateRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _db.Locations.AnyAsync(x => x.Id == dto.LocationId, cancellationToken))
            throw new InvalidOperationException("The selected location does not exist.");

        if (dto.Items.Count == 0)
            throw new InvalidOperationException("At least one material item is required.");

        var entity = new ReplenishmentRequest
        {
            LocationId = dto.LocationId,
            Priority = dto.Priority,
            Status = RequestStatus.Draft,
            StockValidationStatus = StockValidationStatus.NotStarted,
            CreatedAtUtc = DateTime.UtcNow,
            Items = dto.Items.Select(x => new ReplenishmentRequestItem
            {
                ArticleNumber = x.ArticleNumber.Trim(),
                Description = x.Description.Trim(),
                RequestedQuantity = x.RequestedQuantity,
                FulfilledQuantity = 0
            }).ToList()
        };

        _db.ReplenishmentRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(entity).Reference(x => x.Location).LoadAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<RequestResponseDto> SubmitAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetTrackedRequestAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (entity.Status != RequestStatus.Draft)
            throw new InvalidOperationException("Only draft requests can be submitted.");

        if (entity.Items.Count == 0)
            throw new InvalidOperationException("A request must contain at least one item.");

        entity.Status = RequestStatus.Submitted;
        entity.StockValidationStatus = StockValidationStatus.InProgress;
        entity.StockValidationMessage = "Stock validation is in progress.";
        entity.SubmittedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<RequestResponseDto> ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetTrackedRequestAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (entity.Status != RequestStatus.Submitted)
            throw new InvalidOperationException("Only submitted requests can be approved.");

        if (entity.StockValidationStatus != StockValidationStatus.Passed)
            throw new InvalidOperationException("The request can only be approved after stock validation passes.");

        entity.Status = RequestStatus.Approved;
        entity.ApprovedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<RequestResponseDto> RejectAsync(int id, RejectRequestDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetTrackedRequestAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (entity.Status != RequestStatus.Submitted)
            throw new InvalidOperationException("Only submitted requests can be rejected.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("A rejection reason is required.");

        entity.Status = RequestStatus.Rejected;
        entity.RejectionReason = dto.Reason.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<RequestResponseDto> FulfillAsync(int id, FulfillRequestDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetTrackedRequestAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (entity.Status != RequestStatus.Approved)
            throw new InvalidOperationException("Only approved requests can be fulfilled.");

        foreach (var requested in entity.Items)
        {
            var input = dto.Items.FirstOrDefault(x => x.ItemId == requested.Id);

            if (input == null)
                continue;

            if (input.FulfilledQuantity < 0 || input.FulfilledQuantity > requested.RequestedQuantity)
                throw new InvalidOperationException(
                    $"Fulfilled quantity for {requested.ArticleNumber} must be between 0 and {requested.RequestedQuantity}.");

            requested.FulfilledQuantity = input.FulfilledQuantity;
        }

        if (entity.Items.Any(x => x.FulfilledQuantity < x.RequestedQuantity))
            throw new InvalidOperationException("All requested quantities must be fulfilled before completing the request.");

        entity.Status = RequestStatus.Fulfilled;
        entity.FulfilledAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task ApplyStockValidationResultAsync(
        int requestId,
        bool available,
        string message,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ReplenishmentRequests
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);

        if (entity == null || entity.Status != RequestStatus.Submitted)
            return;

        entity.StockValidationStatus = available
            ? StockValidationStatus.Passed
            : StockValidationStatus.Failed;

        entity.StockValidationMessage = message;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ReplenishmentRequest?> GetTrackedRequestAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.ReplenishmentRequests
            .Include(x => x.Location)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private static RequestResponseDto ToDto(ReplenishmentRequest x) => new()
    {
        Id = x.Id,
        LocationId = x.LocationId,
        LocationCode = x.Location?.Code ?? string.Empty,
        LocationName = x.Location?.Name ?? string.Empty,
        Priority = x.Priority,
        Status = x.Status,
        StockValidationStatus = x.StockValidationStatus,
        StockValidationMessage = x.StockValidationMessage,
        RejectionReason = x.RejectionReason,
        CreatedAtUtc = x.CreatedAtUtc,
        SubmittedAtUtc = x.SubmittedAtUtc,
        ApprovedAtUtc = x.ApprovedAtUtc,
        FulfilledAtUtc = x.FulfilledAtUtc,
        Items = x.Items.Select(i => new RequestItemResponseDto
        {
            Id = i.Id,
            ArticleNumber = i.ArticleNumber,
            Description = i.Description,
            RequestedQuantity = i.RequestedQuantity,
            FulfilledQuantity = i.FulfilledQuantity
        }).ToList()
    };
}
