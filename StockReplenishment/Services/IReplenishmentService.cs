using StockReplenishment.DTOs;
using StockReplenishment.Models;

namespace StockReplenishment.Services;

public interface IReplenishmentService
{
    Task<PagedResultDto<RequestResponseDto>> GetRequestsAsync(
        RequestStatus? status,
        RequestPriority? priority,
        int? locationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<RequestResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RequestResponseDto> CreateAsync(CreateRequestDto dto, CancellationToken cancellationToken = default);
    Task<RequestResponseDto> SubmitAsync(int id, CancellationToken cancellationToken = default);
    Task<RequestResponseDto> ApproveAsync(int id, CancellationToken cancellationToken = default);
    Task<RequestResponseDto> RejectAsync(int id, RejectRequestDto dto, CancellationToken cancellationToken = default);
    Task<RequestResponseDto> FulfillAsync(int id, FulfillRequestDto dto, CancellationToken cancellationToken = default);
    Task ApplyStockValidationResultAsync(int requestId, bool available, string message, CancellationToken cancellationToken = default);
}
