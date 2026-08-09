using SIPV2.DataModels;

namespace SIPV2.OccupancyApi.Services;

public interface IParkingRepository
{
    /// <summary>Catálogo completo de parkings (MDParking), ordenado por nombre.</summary>
    Task<IReadOnlyList<Mdparking>> GetAllAsync();

    /// <summary>Busca un parking por su id (MDParking.ID). Null si no existe.</summary>
    Task<Mdparking?> GetByIdAsync(string id);
}
