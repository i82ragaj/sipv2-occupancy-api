using SIPV2.DataModels;
using SIPV2.OccupancyApi.Services;

namespace SIPV2.OccupancyApi.Tests.Fakes;

public class FakeParkingRepository : IParkingRepository
{
    public List<Mdparking> Parkings { get; } = [];

    public Task<IReadOnlyList<Mdparking>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Mdparking>>(Parkings.OrderBy(p => p.Name).ToList());

    public Task<Mdparking?> GetByIdAsync(string id) =>
        Task.FromResult(Parkings.FirstOrDefault(p => p.Id == id));
}
