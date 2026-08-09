using Microsoft.EntityFrameworkCore;
using SIPV2.DataModels;

namespace SIPV2.OccupancyApi.Services;

public class EfOccupancyRepository : IOccupancyRepository
{
    private readonly AppDbContext _db;

    public EfOccupancyRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<VoccupationActual>> GetCurrentAsync(string? parkingId, string? counterName)
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

        return await query
            .OrderBy(o => o.ParkingName)
            .ThenBy(o => o.CounterName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<VoccupationActual>> GetByCounterIdsAsync(IReadOnlyList<string> counterIds) =>
        await _db.VoccupationActuals
            .AsNoTracking()
            .Where(o => counterIds.Contains(o.CounterId))
            .OrderBy(o => o.ParkingName)
            .ThenBy(o => o.CounterName)
            .ToListAsync();

    public async Task<IReadOnlyList<VoccupationActual>> GetByParkingIdAsync(string parkingId) =>
        await _db.VoccupationActuals
            .AsNoTracking()
            .Where(o => o.ParkingId == parkingId)
            .OrderBy(o => o.CounterName)
            .ToListAsync();
}
