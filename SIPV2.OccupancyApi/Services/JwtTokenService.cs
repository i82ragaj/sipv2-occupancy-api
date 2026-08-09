using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SIPV2.DataModels;

namespace SIPV2.OccupancyApi.Services;

/// <summary>Emite JWTs firmados con HMAC-SHA256 a partir de la configuración "Jwt" (Key/Issuer/Audience/ExpiryMinutes).</summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(Mduser user, IReadOnlyList<string> roles)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException("Falta la configuración Jwt:Key.");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expiryMinutes = jwtSection.GetValue("ExpiryMinutes", 60);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Login),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", $"{user.Name} {user.LastName}".Trim()),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
