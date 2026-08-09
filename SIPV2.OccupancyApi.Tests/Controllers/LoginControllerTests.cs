using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Contracts;
using SIPV2.OccupancyApi.Controllers;
using SIPV2.OccupancyApi.Services;
using SIPV2.OccupancyApi.Tests.Fakes;

namespace SIPV2.OccupancyApi.Tests.Controllers;

public class LoginControllerTests
{
    private static IJwtTokenService CreateJwtTokenService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-signing-key-at-least-32-chars-long",
                ["Jwt:Issuer"] = "SIPV2.OccupancyApi.Tests",
                ["Jwt:Audience"] = "SIPV2.OccupancyApi.Tests",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();
        return new JwtTokenService(config);
    }

    /// <summary>Da de alta un usuario activo con password hasheada (BCrypt) y, opcionalmente, un rol activo.</summary>
    private static Mduser SeedUser(FakeUserRepository users, string login, string plainPassword, bool active = true, string? roleName = "APIWEB")
    {
        var user = new Mduser
        {
            Id = Guid.NewGuid(),
            Login = login,
            Password = BCrypt.Net.BCrypt.HashPassword(plainPassword),
            Name = "Test",
            Active = active,
        };

        if (roleName is not null)
        {
            var role = new Mdrol { Id = Guid.NewGuid(), Name = roleName, Active = true };
            user.MduserRols.Add(new MduserRol { Id = Guid.NewGuid(), UserId = user.Id, RolId = role.Id, Active = true, Rol = role });
        }

        users.Users.Add(user);
        return user;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndRoles()
    {
        var users = new FakeUserRepository();
        SeedUser(users, "gooduser", "CorrectPass1!");
        var controller = new LoginController(users, CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest("gooduser", "CorrectPass1!"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("gooduser", response.Login);
        Assert.Equal(["APIWEB"], response.Roles);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var users = new FakeUserRepository();
        SeedUser(users, "gooduser", "CorrectPass1!");
        var controller = new LoginController(users, CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest("gooduser", "WrongPassword"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithUnknownLogin_ReturnsUnauthorized()
    {
        var controller = new LoginController(new FakeUserRepository(), CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest("no-existe", "cualquiera"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        var users = new FakeUserRepository();
        SeedUser(users, "inactivo", "CorrectPass1!", active: false);
        var controller = new LoginController(users, CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest("inactivo", "CorrectPass1!"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("", "algo")]
    [InlineData("algo", "")]
    [InlineData(null, null)]
    public async Task Login_WithMissingCredentials_ReturnsBadRequest(string? login, string? password)
    {
        var controller = new LoginController(new FakeUserRepository(), CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest(login!, password!));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_UserWithoutRoles_ReturnsEmptyRolesList()
    {
        var users = new FakeUserRepository();
        SeedUser(users, "sinrol", "CorrectPass1!", roleName: null);
        var controller = new LoginController(users, CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest("sinrol", "CorrectPass1!"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Empty(response.Roles);
    }
}
