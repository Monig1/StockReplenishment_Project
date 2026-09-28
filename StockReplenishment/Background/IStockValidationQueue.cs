namespace StockReplenishment.Background;

public interface IStockValidationQueue
{
    ValueTask QueueAsync(int requestId, CancellationToken cancellationToken = default);
    ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
}
