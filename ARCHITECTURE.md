# Plantilla de estructura y arquitectura — API .NET con capa de repositorio

> Generalización de la arquitectura usada en este repo (`SIPV2.DataModels` / `SIPV2.OccupancyApi` / `SIPV2.OccupancyApi.Tests`), pensada para reutilizar como punto de partida en otros proyectos .NET similares.

## Estructura de carpetas

```
<solucion>.slnx                          # formato .slnx (no .sln clásico)

<Proyecto>.DataModels/                   # Class library — capa de acceso a datos (opcional: submódulo git aparte)
├── Data/
│   └── AppDbContext.cs                  # generado por EF Core scaffold — NO editar a mano
└── Models/                              # un archivo por tabla/vista, generado por scaffold
    └── (partial classes aparte para customizaciones, ver OnModelCreatingPartial)

<Proyecto>.Api/                          # ASP.NET Core Web API — la app real
├── Program.cs                           # DI, auth, Swagger, pipeline de middleware
├── Controllers/                         # un controlador por recurso/dominio
│   ├── LoginController.cs               # [AllowAnonymous] — emite JWT
│   ├── RecursoAController.cs            # [Authorize(Roles = "...")]
│   └── RecursoBController.cs
├── Services/                            # lógica de infraestructura + repositorios
│   ├── IJwtTokenService.cs / JwtTokenService.cs
│   ├── I<Recurso>Repository.cs          # interfaz — lo que ve el controller
│   └── Ef<Recurso>Repository.cs         # implementación real sobre AppDbContext
├── Contracts/                           # DTOs de request/response — nunca exponer entidades EF
├── appsettings.json                     # placeholders vacíos (connection string, JWT key)
└── appsettings.Development.json         # valores reales de desarrollo

<Proyecto>.Api.Tests/                    # xUnit — controllers probados en proceso, sin DB
├── Fakes/                               # Fake<Recurso>Repository : I<Recurso>Repository (List<T> en memoria)
├── Controllers/                         # un archivo de tests por controller
└── Services/                            # tests de servicios aislados (p.ej. JwtTokenService)
```

## Principios de arquitectura

1. **Controladores no tocan `DbContext` directamente.** Dependen de interfaces `I<Recurso>Repository`, implementadas por `Ef<Recurso>Repository` (que sí usa EF/LINQ). Esto permite testear controladores con fakes en memoria, sin base de datos ni `EF Core InMemory` — clave si hay entidades `[Keyless]` (vistas de solo lectura), que no se pueden sembrar (`Add`/`SaveChanges`) en ningún proveedor de EF.

2. **DTOs propios en `Contracts/`.** Nunca se devuelven entidades EF-scaffoldadas tal cual; siempre se mapea a un DTO explícito, aunque sea 1:1. Evita acoplar el contrato público al esquema de la base de datos.

3. **Autenticación JWT + rol por defecto ("secure by default").**
   - `LoginController` verifica credenciales (hash con BCrypt), emite JWT firmado (HS256) con claims `sub`, `unique_name`, `role`×N.
   - `Program.cs` registra un `FallbackPolicy` en `AddAuthorization` que exige un rol concreto — cualquier endpoint sin `[Authorize]`/`[AllowAnonymous]` explícito queda protegido igualmente.
   - Ojo: si un controller pone *cualquier* `[Authorize]`, sale del fallback policy → hay que repetir el rol explícitamente (`[Authorize(Roles = "...")]`) en cada uno.

4. **Modelos generados, nunca editados a mano.** Todo lo scaffoldado desde la DB (`Models/`, `AppDbContext.cs`) se regenera con `dotnet ef dbcontext scaffold --force`; las customizaciones van en `partial class` aparte, enganchadas vía el hook `OnModelCreatingPartial`.

5. **Config dividida por entorno.** `appsettings.json` con placeholders vacíos que fuerzan un fallo temprano si falta algo (p.ej. `Program.cs` lanza excepción si no hay `Jwt:Key`); secretos reales solo en `appsettings.Development.json` (o `dotnet user-secrets`/variables de entorno en producción).

6. **Tests sin infraestructura real.** El proyecto de tests referencia la API directamente e instancia controladores en proceso — sin `WebApplicationFactory`, sin HTTP, sin base de datos. Cada repositorio tiene un *fake* hermano (`List<T>` detrás de la misma interfaz) en `Tests/Fakes/`; al añadir un método a una interfaz de repositorio hay que añadirlo también al fake — no hay nada que lo enlace a nivel de compilador salvo la interfaz compartida.

## Flujo de auth (patrón típico)

1. `POST /login` → busca usuario activo, verifica password con BCrypt, recoge roles vía tabla puente (many-to-many).
2. Se firma un JWT con los claims necesarios y expiración configurable.
3. El middleware `AddJwtBearer` valida el token en cada request; `AddAuthorization` aplica el rol mínimo global.
4. No hay endpoint de registro/reset — las cuentas se siembran directamente en BD con el hash ya calculado.
