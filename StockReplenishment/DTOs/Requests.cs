using System.ComponentModel.DataAnnotations;
using StockReplenishment.Models;

namespace StockReplenishment.DTOs;

public class CreateRequestDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int LocationId { get; set; }

    public RequestPriority Priority { get; set; } = RequestPriority.Normal;

    [MinLength(1)]
    public List<CreateRequestItemDto> Items { get; set; } = new();
}

public class CreateRequestItemDto
{
    [Required, MaxLength(50)]
    public string ArticleNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int RequestedQuantity { get; set; }
}

public class RejectRequestDto
{
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class FulfillRequestDto
{
    [MinLength(1)]
    public List<FulfillItemDto> Items { get; set; } = new();
}

public class FulfillItemDto
{
    public int ItemId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
    public int FulfilledQuantity { get; set; }
}

public class RequestResponseDto
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public RequestPriority Priority { get; set; }
    public RequestStatus Status { get; set; }
    public StockValidationStatus StockValidationStatus { get; set; }
    public string? StockValidationMessage { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? FulfilledAtUtc { get; set; }
    public List<RequestItemResponseDto> Items { get; set; } = new();
}

public class RequestItemResponseDto
{
    public int Id { get; set; }
    public string ArticleNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int FulfilledQuantity { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
