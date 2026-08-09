using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Contracts;

namespace SIPV2.OccupancyApi.Controllers;

[Authorize(Roles = "APIWEB")]
[ApiController]
[Route("api/[controller]")]
public class ParkingController : ControllerBase
{
    private readonly AppDbContext _db;

    public ParkingController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Catálogo de parkings (MDParking).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ParkingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ParkingDto>>> GetParkings()
    {
        var parkings = await _db.Mdparkings
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ParkingDto(p.Id, p.Name ?? p.Id))
            .ToListAsync();

        return Ok(parkings);
    }

    /// <summary>Detalle de un parking: código, nombre y el listado completo de sus contadores (vista VOccupationActual).</summary>
    /// <param name="id">Código del parking (MDParking.ID).</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ParkingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParkingDetailDto>> GetParkingDetail(string id)
    {
        var counters = await _db.VoccupationActuals
            .AsNoTracking()
            .Where(o => o.ParkingId == id)
            .OrderBy(o => o.CounterName)
            .Select(o => new CounterDto(o.CounterId, o.CounterCode, o.CounterName, o.Capacity, o.CurrentLevel, o.Perc, o.Updated))
            .ToListAsync();

        if (counters.Count > 0)
        {
            var parkingName = await _db.VoccupationActuals
                .AsNoTracking()
                .Where(o => o.ParkingId == id)
                .Select(o => o.ParkingName)
                .FirstAsync();

            return Ok(new ParkingDetailDto(id, parkingName ?? id, counters));
        }

        // Sin contadores en la vista de ocupación: comprobamos si el parking existe siquiera.
        var parking = await _db.Mdparkings.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (parking is null)
        {
            return NotFound();
        }

        return Ok(new ParkingDetailDto(parking.Id, parking.Name ?? parking.Id, []));
    }
}
