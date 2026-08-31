using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using SIPV2.DataModels;
using SIPV2.OccupancyApi.Services;

// Bootstrap logger: cubre errores durante el arranque, antes de que builder.Host.UseSerilog()
// configure el logger definitivo (con los sinks/niveles de la sección "Serilog" de appsettings).
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Niveles y sinks (consola, fichero: ruta, rotación, retención...) se configuran
    // enteramente desde la sección "Serilog" de appsettings*.json — ver ese fichero
    // para cambiarlos sin tocar código. Aquí solo se añade el enriquecimiento común.
    builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(context.Configuration));

    // Add services to the container.

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "ESParking SIPV2 Occupancy API", Version = "v1" });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Pega solo el token (sin el prefijo \"Bearer \"), obtenido de POST /api/login.",
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });
    });

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<IParkingRepository, EfParkingRepository>();
    builder.Services.AddScoped<IOccupancyRepository, EfOccupancyRepository>();

    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSection["Key"]
        ?? throw new InvalidOperationException("Falta la configuración Jwt:Key.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(1),
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // Cualquier endpoint sin [Authorize]/[AllowAnonymous] explícito exige el rol apiweb o admin
        // por defecto, para que los controllers nuevos queden protegidos aunque alguien olvide anotarlos.
        // Ojo: los nombres de rol deben coincidir en mayúsculas/minúsculas exactas con MDRol.Name
        // (comparación case-sensitive del claim), que en la base de datos están en minúscula.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole("apiweb", "admin")
            .Build();
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    // Swagger siempre en Development; en otros entornos (p. ej. un IIS de pruebas/QA) solo si
    // se activa explícitamente vía config — así un despliegue de producción real puede dejarlo
    // apagado sin tocar código (Swagger:Enabled=false por defecto en appsettings.json).
    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(); // http://{host}/swagger
    }

    app.UseHttpsRedirection();

    // Log de eventos de negocio: accesos rechazados por falta/insuficiencia de autenticación o rol.
    app.Use(async (context, next) =>
    {
        await next();

        if (context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            Log.Warning(
                "Acceso no autorizado: {Method} {Path} -> {StatusCode}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode);
        }
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SIPV2.OccupancyApi terminó de forma inesperada durante el arranque");
}
finally
{
    Log.CloseAndFlush();
}
