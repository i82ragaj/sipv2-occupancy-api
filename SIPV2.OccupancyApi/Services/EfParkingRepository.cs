using Microsoft.EntityFrameworkCore;
using SIPV2.DataModels;

namespace SIPV2.OccupancyApi.Services;

public class EfParkingRepository : IParkingRepository
{
    private readonly AppDbContext _db;

    public EfParkingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Mdparking>> GetAllAsync() =>
        await _db.Mdparkings
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

    public Task<Mdparking?> GetByIdAsync(string id) =>
        _db.Mdparkings.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
}
