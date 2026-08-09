using SIPV2.DataModels;

namespace SIPV2.OccupancyApi.Services;

public interface IJwtTokenService
{
    /// <summary>Genera un JWT firmado para el usuario indicado. Devuelve el token y su fecha de expiración (UTC).</summary>
    (string Token, DateTime ExpiresAtUtc) GenerateToken(Mduser user, IReadOnlyList<string> roles);
}
