using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Contracts;

namespace SIPV2.OccupancyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OccupancyController : ControllerBase
{
    private readonly AppDbContext _db;

    public OccupancyController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Catálogo de parkings (MDParking).</summary>
    /// <param name="onlyActive">Si es true (por defecto), devuelve solo los parkings activos.</param>
    [HttpGet("parkings")]
    [ProducesResponseType(typeof(IEnumerable<ParkingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ParkingDto>>> GetParkings([FromQuery] bool onlyActive = true)
    {
        var query = _db.Mdparkings.AsNoTracking();
        if (onlyActive)
        {
            query = query.Where(p => p.Active);
        }

        var parkings = await query
            .OrderBy(p => p.Name)
            .Select(p => new ParkingDto(p.Id, p.Name ?? p.Id, p.Type, p.Active))
            .ToListAsync();

        return Ok(parkings);
    }

    /// <summary>Ocupación actual por contador/parking (vista VOccupationActual).</summary>
    /// <param name="parkingCode">Filtra por código de parking (MDParking.ID); opcional.</param>
    [HttpGet("current")]
    [ProducesResponseType(typeof(IEnumerable<CurrentOccupancyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CurrentOccupancyDto>>> GetCurrentOccupancy([FromQuery] string? parkingCode = null)
    {
        var query = _db.VoccupationActuals.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(parkingCode))
        {
            query = query.Where(o => o.ParkingCode == parkingCode);
        }

        var occupancy = await query
            .OrderBy(o => o.ParkingName)
            .ThenBy(o => o.CounterName)
            .Select(o => new CurrentOccupancyDto(
                o.ParkingCode,
                o.ParkingName,
                o.CounterCode,
                o.CounterName,
                o.Capacity,
                o.CurrentLevel,
                o.Perc,
                o.Updated))
            .ToListAsync();

        return Ok(occupancy);
    }
}
