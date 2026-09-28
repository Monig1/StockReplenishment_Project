namespace StockReplenishment.Models;

public class ReplenishmentRequestItem
{
    public int Id { get; set; }

    public int ReplenishmentRequestId { get; set; }
    public ReplenishmentRequest ReplenishmentRequest { get; set; } = null!;

    public string ArticleNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int FulfilledQuantity { get; set; }
}
