using Microsoft.EntityFrameworkCore;
using SIPV2.DataModels;

namespace SIPV2.OccupancyApi.Services;

public class EfUserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public EfUserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Mduser?> FindActiveByLoginAsync(string login) =>
        _db.Mdusers
            .Include(u => u.MduserRols)
                .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u => u.Login == login && u.Active);
}
