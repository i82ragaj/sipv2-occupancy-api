using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Contracts;
using SIPV2.OccupancyApi.Services;

namespace SIPV2.OccupancyApi.Controllers;

[Authorize(Roles = "apiweb,admin")]
[ApiController]
[Route("api/[controller]")]
public class OccupancyController : ControllerBase
{
    private readonly IOccupancyRepository _occupancy;

    public OccupancyController(IOccupancyRepository occupancy)
    {
        _occupancy = occupancy;
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
        var occupancy = await _occupancy.GetCurrentAsync(parkingId, counterName);
        return Ok(occupancy.Select(ToDto));
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

        var occupancy = await _occupancy.GetByCounterIdsAsync(counterIds);
        return Ok(occupancy.Select(ToDto));
    }

    private static CurrentOccupancyDto ToDto(VoccupationActual o) => new(
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
