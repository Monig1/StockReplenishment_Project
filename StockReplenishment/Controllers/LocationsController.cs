using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockReplenishment.Data;
using StockReplenishment.Models;

namespace StockReplenishment.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LocationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Location>>> Get(CancellationToken cancellationToken)
    {
        var locations = await _db.Locations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(locations);
    }
}
