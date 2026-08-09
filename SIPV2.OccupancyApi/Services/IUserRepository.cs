using SIPV2.DataModels;

namespace SIPV2.OccupancyApi.Services;

public interface IUserRepository
{
    /// <summary>Busca un usuario activo por login, con sus roles (MduserRol → Mdrol) ya cargados.</summary>
    Task<Mduser?> FindActiveByLoginAsync(string login);
}
