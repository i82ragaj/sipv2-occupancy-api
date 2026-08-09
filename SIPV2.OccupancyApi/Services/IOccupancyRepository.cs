using SIPV2.DataModels;

namespace SIPV2.OccupancyApi.Services;

public interface IOccupancyRepository
{
    /// <summary>Ocupación actual (VOccupationActual), filtrada opcionalmente por parkingId y/o nombre de contador (parcial, sin distinguir mayúsculas/minúsculas).</summary>
    Task<IReadOnlyList<VoccupationActual>> GetCurrentAsync(string? parkingId, string? counterName);

    /// <summary>Ocupación actual de un listado concreto de contadores (por CounterId).</summary>
    Task<IReadOnlyList<VoccupationActual>> GetByCounterIdsAsync(IReadOnlyList<string> counterIds);

    /// <summary>Todos los contadores de un parking concreto.</summary>
    Task<IReadOnlyList<VoccupationActual>> GetByParkingIdAsync(string parkingId);
}
