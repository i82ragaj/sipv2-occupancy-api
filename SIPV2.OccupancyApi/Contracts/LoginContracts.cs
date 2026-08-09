namespace SIPV2.OccupancyApi.Contracts;

/// <summary>Credenciales enviadas por el cliente para autenticarse.</summary>
public record LoginRequest(string Login, string Password);

/// <summary>Respuesta de un login correcto: token JWT y metadatos del usuario.</summary>
public record LoginResponse(string Token, DateTime ExpiresAtUtc, string Login, IReadOnlyList<string> Roles);
