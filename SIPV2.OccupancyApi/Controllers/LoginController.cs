using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIPV2.OccupancyApi.Contracts;
using SIPV2.OccupancyApi.Services;

namespace SIPV2.OccupancyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginController> _logger;

    public LoginController(IUserRepository users, IJwtTokenService jwtTokenService, ILogger<LoginController> logger)
    {
        _users = users;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>Autentica un usuario de MDUser y devuelve un JWT si las credenciales son válidas.</summary>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Login y password son obligatorios.");
        }

        var user = await _users.FindActiveByLoginAsync(request.Login);

        if (user is null || user.Password is null || !VerifyPassword(request.Password, user.Password))
        {
            _logger.LogWarning("Login fallido para el usuario '{Login}': credenciales inválidas o usuario inactivo.", request.Login);
            return Unauthorized("Credenciales inválidas.");
        }

        var roles = user.MduserRols
            .Where(ur => ur.Active && ur.Rol is { Active: true, Name: not null })
            .Select(ur => ur.Rol!.Name!)
            .Distinct()
            .ToList();

        var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(user, roles);

        _logger.LogInformation("Login correcto para el usuario '{Login}' con roles [{Roles}].", user.Login, string.Join(", ", roles));

        return Ok(new LoginResponse(token, expiresAtUtc, user.Login, roles));
    }

    /// <summary>Compara la contraseña recibida contra el hash BCrypt guardado en MDUser.Password.</summary>
    private static bool VerifyPassword(string plainPassword, string storedHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, storedHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // El hash guardado no tiene formato BCrypt válido (p. ej. dato legado en texto plano).
            return false;
        }
    }
}
