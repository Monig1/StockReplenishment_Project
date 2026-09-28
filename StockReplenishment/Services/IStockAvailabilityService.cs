namespace StockReplenishment.Services;

public interface IStockAvailabilityService
{
    Task<StockValidationResult> CheckAvailabilityAsync(
        int locationId,
        IReadOnlyCollection<(string ArticleNumber, int Quantity)> items,
        CancellationToken cancellationToken = default);
}
