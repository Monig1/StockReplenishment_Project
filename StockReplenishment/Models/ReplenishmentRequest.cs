namespace StockReplenishment.Models;

public class ReplenishmentRequest
{
    public int Id { get; set; }

    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public RequestPriority Priority { get; set; }
    public RequestStatus Status { get; set; }

    public StockValidationStatus StockValidationStatus { get; set; }
    public string? StockValidationMessage { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? FulfilledAtUtc { get; set; }

    public ICollection<ReplenishmentRequestItem> Items { get; set; } = new List<ReplenishmentRequestItem>();
}
