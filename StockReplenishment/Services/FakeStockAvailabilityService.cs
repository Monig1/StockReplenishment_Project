namespace StockReplenishment.Services;

public class FakeStockAvailabilityService : IStockAvailabilityService
{
    private readonly Random _random = new();

    public async Task<StockValidationResult> CheckAvailabilityAsync(
        int locationId,
        IReadOnlyCollection<(string ArticleNumber, int Quantity)> items,
        CancellationToken cancellationToken = default)
    {
        var delay = _random.Next(3000, 6001);
        await Task.Delay(delay, cancellationToken);

        // Deterministic demo rule: an article ending with 999 is unavailable.
        var unavailable = items.FirstOrDefault(x => x.ArticleNumber.EndsWith("999", StringComparison.OrdinalIgnoreCase));

        if (unavailable != default)
        {
            return new StockValidationResult(
                false,
                $"Insufficient stock for article {unavailable.ArticleNumber}.");
        }

        return new StockValidationResult(
            true,
            "Stock is available for all requested items.");
    }
}
