using SIPV2.DataModels;
using SIPV2.OccupancyApi.Services;

namespace SIPV2.OccupancyApi.Tests.Fakes;

public class FakeOccupancyRepository : IOccupancyRepository
{
    public List<VoccupationActual> Data { get; } = [];

    public Task<IReadOnlyList<VoccupationActual>> GetCurrentAsync(string? parkingId, string? counterName)
    {
        IEnumerable<VoccupationActual> query = Data;

        if (!string.IsNullOrWhiteSpace(parkingId))
        {
            query = query.Where(o => o.ParkingId == parkingId);
        }

        if (!string.IsNullOrWhiteSpace(counterName))
        {
            query = query.Where(o => o.CounterName?.Contains(counterName, StringComparison.OrdinalIgnoreCase) == true);
        }

        return Task.FromResult<IReadOnlyList<VoccupationActual>>(
            query.OrderBy(o => o.ParkingName).ThenBy(o => o.CounterName).ToList());
    }

    public Task<IReadOnlyList<VoccupationActual>> GetByCounterIdsAsync(IReadOnlyList<string> counterIds) =>
        Task.FromResult<IReadOnlyList<VoccupationActual>>(
            Data.Where(o => counterIds.Contains(o.CounterId))
                .OrderBy(o => o.ParkingName).ThenBy(o => o.CounterName)
                .ToList());

    public Task<IReadOnlyList<VoccupationActual>> GetByParkingIdAsync(string parkingId) =>
        Task.FromResult<IReadOnlyList<VoccupationActual>>(
            Data.Where(o => o.ParkingId == parkingId).OrderBy(o => o.CounterName).ToList());
}
