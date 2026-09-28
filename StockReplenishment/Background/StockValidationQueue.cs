using System.Threading.Channels;

namespace StockReplenishment.Background;

public class StockValidationQueue : IStockValidationQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public ValueTask QueueAsync(int requestId, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(requestId, cancellationToken);

    public ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}
