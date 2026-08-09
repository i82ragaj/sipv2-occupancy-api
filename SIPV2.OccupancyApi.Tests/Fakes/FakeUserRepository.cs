using SIPV2.DataModels;
using SIPV2.OccupancyApi.Services;

namespace SIPV2.OccupancyApi.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    public List<Mduser> Users { get; } = [];

    public Task<Mduser?> FindActiveByLoginAsync(string login) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Login == login && u.Active));
}
