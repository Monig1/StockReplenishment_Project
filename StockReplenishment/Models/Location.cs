namespace StockReplenishment.Models;

public class Location
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<ReplenishmentRequest> Requests { get; set; } = new List<ReplenishmentRequest>();
}
