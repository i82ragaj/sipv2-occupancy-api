using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Contracts;

namespace SIPV2.OccupancyApi.Controllers;

[Authorize(Roles = "APIWEB")]
[ApiController]
[Route("api/[controller]")]
public class OccupancyController : ControllerBase
{
    private readonly AppDbContext _db;

    public OccupancyController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Ocupación actual por contador/parking (vista VOccupationActual).</summary>
    /// <param name="parkingId">Filtra por id de parking (MDParking.ID); opcional.</param>
    /// <param name="counterName">Filtra por nombre de contador (coincidencia parcial, sin distinguir mayúsculas/minúsculas); opcional.</param>
    [HttpGet("current")]
    [ProducesResponseType(typeof(IEnumerable<CurrentOccupancyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CurrentOccupancyDto>>> GetCurrentOccupancy(
        [FromQuery] string? parkingId = null,
        [FromQuery] string? counterName = null)
    {
        var query = _db.VoccupationActuals.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(parkingId))
        {
            query = query.Where(o => o.ParkingId == parkingId);
        }

        if (!string.IsNullOrWhiteSpace(counterName))
        {
            query = query.Where(o => o.CounterName != null && EF.Functions.Like(o.CounterName, $"%{counterName}%"));
        }

        var occupancy = await query
            .OrderBy(o => o.ParkingName)
            .ThenBy(o => o.CounterName)
            .Select(ToDto)
            .ToListAsync();

        return Ok(occupancy);
    }

    /// <summary>Ocupación actual de un listado concreto de contadores, por su CounterId.</summary>
    [HttpPost("current")]
    [ProducesResponseType(typeof(IEnumerable<CurrentOccupancyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<CurrentOccupancyDto>>> GetCurrentOccupancyByCounterIds(
        [FromBody] CurrentOccupancyByCounterIdsRequest request)
    {
        var counterIds = request.CounterIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList() ?? [];
        if (counterIds.Count == 0)
        {
            return BadRequest("counterIds no puede estar vacío.");
        }

        var occupancy = await _db.VoccupationActuals
            .AsNoTracking()
            .Where(o => counterIds.Contains(o.CounterId))
            .OrderBy(o => o.ParkingName)
            .ThenBy(o => o.CounterName)
            .Select(ToDto)
            .ToListAsync();

        return Ok(occupancy);
    }

    private static readonly System.Linq.Expressions.Expression<Func<VoccupationActual, CurrentOccupancyDto>> ToDto = o => new CurrentOccupancyDto(
        o.ParkingId,
        o.ParkingName ?? o.ParkingId,
        o.CounterId,
        o.CounterCode,
        o.CounterName,
        o.Capacity,
        o.CurrentLevel,
        o.Perc,
        o.Updated);
}
