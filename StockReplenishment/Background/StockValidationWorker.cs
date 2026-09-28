using Microsoft.EntityFrameworkCore;
using StockReplenishment.Data;
using StockReplenishment.Services;

namespace StockReplenishment.Background;

public class StockValidationWorker : BackgroundService
{
    private readonly IStockValidationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockValidationWorker> _logger;

    public StockValidationWorker(
        IStockValidationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<StockValidationWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var requestId = await _queue.DequeueAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var stockService = scope.ServiceProvider.GetRequiredService<IStockAvailabilityService>();
                var replenishmentService = scope.ServiceProvider.GetRequiredService<IReplenishmentService>();

                var request = await db.ReplenishmentRequests
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == requestId, stoppingToken);

                if (request == null || request.Status != Models.RequestStatus.Submitted)
                    continue;

                var result = await stockService.CheckAvailabilityAsync(
                    request.LocationId,
                    request.Items.Select(x => (x.ArticleNumber, x.RequestedQuantity)).ToList(),
                    stoppingToken);

                await replenishmentService.ApplyStockValidationResultAsync(
                    requestId,
                    result.Available,
                    result.Message,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock validation worker failed.");
            }
        }
    }
}
