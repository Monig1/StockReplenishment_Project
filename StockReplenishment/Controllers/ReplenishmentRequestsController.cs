using Microsoft.AspNetCore.Mvc;
using StockReplenishment.Background;
using StockReplenishment.DTOs;
using StockReplenishment.Models;
using StockReplenishment.Services;

namespace StockReplenishment.Controllers;

[ApiController]
[Route("api/replenishment-requests")]
public class ReplenishmentRequestsController : ControllerBase
{
    private readonly IReplenishmentService _service;
    private readonly IStockValidationQueue _queue;

    public ReplenishmentRequestsController(
        IReplenishmentService service,
        IStockValidationQueue queue)
    {
        _service = service;
        _queue = queue;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<RequestResponseDto>>> GetAll(
        [FromQuery] RequestStatus? status,
        [FromQuery] RequestPriority? priority,
        [FromQuery] int? locationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetRequestsAsync(
            status, priority, locationId, page, pageSize, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RequestResponseDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RequestResponseDto>> Create(
        [FromBody] CreateRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<RequestResponseDto>> Submit(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.SubmitAsync(id, cancellationToken);
            await _queue.QueueAsync(id, cancellationToken);
            return AcceptedAtAction(nameof(GetById), new { id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<RequestResponseDto>> Approve(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.ApproveAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<RequestResponseDto>> Reject(
        int id,
        [FromBody] RejectRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.RejectAsync(id, dto, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/fulfill")]
    public async Task<ActionResult<RequestResponseDto>> Fulfill(
        int id,
        [FromBody] FulfillRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.FulfillAsync(id, dto, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
