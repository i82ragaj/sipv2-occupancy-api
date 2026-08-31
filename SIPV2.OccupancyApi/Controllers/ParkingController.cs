using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Contracts;
using SIPV2.OccupancyApi.Services;

namespace SIPV2.OccupancyApi.Controllers;

[Authorize(Roles = "apiweb,admin")]
[ApiController]
[Route("api/[controller]")]
public class ParkingController : ControllerBase
{
    private readonly IParkingRepository _parkings;
    private readonly IOccupancyRepository _occupancy;

    public ParkingController(IParkingRepository parkings, IOccupancyRepository occupancy)
    {
        _parkings = parkings;
        _occupancy = occupancy;
    }

    /// <summary>Catálogo de parkings (MDParking).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ParkingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ParkingDto>>> GetParkings()
    {
        var parkings = await _parkings.GetAllAsync();
        return Ok(parkings.Select(p => new ParkingDto(p.Id, p.Name ?? p.Id)));
    }

    /// <summary>Detalle de un parking: código, nombre y el listado completo de sus contadores (vista VOccupationActual).</summary>
    /// <param name="id">Código del parking (MDParking.ID).</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ParkingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParkingDetailDto>> GetParkingDetail(string id)
    {
        var counters = await _occupancy.GetByParkingIdAsync(id);

        if (counters.Count > 0)
        {
            var parkingName = counters[0].ParkingName ?? id;
            return Ok(new ParkingDetailDto(id, parkingName, counters.Select(ToCounterDto).ToList()));
        }

        // Sin contadores en la vista de ocupación: comprobamos si el parking existe siquiera.
        var parking = await _parkings.GetByIdAsync(id);
        if (parking is null)
        {
            return NotFound();
        }

        return Ok(new ParkingDetailDto(parking.Id, parking.Name ?? parking.Id, []));
    }

    private static CounterDto ToCounterDto(VoccupationActual o) =>
        new(o.CounterId, o.CounterCode, o.CounterName, o.Capacity, o.CurrentLevel, o.Perc, o.Updated);
}
