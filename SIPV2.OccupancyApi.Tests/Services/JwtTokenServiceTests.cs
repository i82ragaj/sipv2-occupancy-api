using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Services;

namespace SIPV2.OccupancyApi.Tests.Services;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int expiryMinutes = 60)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-signing-key-at-least-32-chars-long",
                ["Jwt:Issuer"] = "SIPV2.OccupancyApi.Tests",
                ["Jwt:Audience"] = "SIPV2.OccupancyApi.Tests",
                ["Jwt:ExpiryMinutes"] = expiryMinutes.ToString(),
            })
            .Build();

        return new JwtTokenService(config);
    }

    private static Mduser CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Login = "someuser",
        Name = "Some",
        LastName = "User",
        Active = true,
    };

    [Fact]
    public void GenerateToken_IncludesExpectedClaims()
    {
        var service = CreateService();
        var user = CreateUser();

        var (token, expiresAtUtc) = service.GenerateToken(user, ["APIWEB", "Admin"]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Login, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal(["APIWEB", "Admin"], jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray());
        Assert.Equal("SIPV2.OccupancyApi.Tests", jwt.Issuer);
        Assert.Equal("SIPV2.OccupancyApi.Tests", jwt.Audiences.Single());
        Assert.Equal(expiresAtUtc, jwt.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GenerateToken_WithNoRoles_ProducesNoRoleClaims()
    {
        var service = CreateService();
        var (token, _) = service.GenerateToken(CreateUser(), []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public void GenerateToken_ExpiresAtUtc_MatchesConfiguredExpiryMinutes()
    {
        var service = CreateService(expiryMinutes: 5);
        var before = DateTime.UtcNow;

        var (_, expiresAtUtc) = service.GenerateToken(CreateUser(), []);

        Assert.InRange(expiresAtUtc, before.AddMinutes(5).AddSeconds(-2), before.AddMinutes(5).AddSeconds(2));
    }

    [Fact]
    public void GenerateToken_MissingKey_Throws()
    {
        var config = new ConfigurationBuilder().Build(); // sin Jwt:Key
        var service = new JwtTokenService(config);

        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(CreateUser(), []));
    }
}
