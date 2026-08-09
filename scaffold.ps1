# ============================================================
# Script de scaffold para generar modelos EF Core desde SQL Server
# Reemplaza los valores entre < > con los datos reales de tu BD
# ============================================================

$servidor    = "192.168.100.73"         # ej: localhost, 192.168.1.10, MIPC\SQLEXPRESS
$baseDatos   = "AA-ADMIN"       # nombre de la base de datos
$usuario     = "wwuser"          # usuario SQL Server
$contrasena  = "CL.2026"       # contraseña (sin caracteres especiales sin escapar)

$connectionString = "Server=$servidor;Database=$baseDatos;User Id=$usuario;Password=$contrasena;TrustServerCertificate=True;"

Set-Location "$PSScriptRoot"

dotnet ef dbcontext scaffold $connectionString `
    Microsoft.EntityFrameworkCore.SqlServer `
    --output-dir Models `
    --context-dir Data `
    --context AppDbContext `
    --namespace SIPV2.DataModels `
    --context-namespace SIPV2.DataModels `
    --project "SIPV2.DataModels\SIPV2.DataModels.csproj" `
    --data-annotations `
    --no-onconfiguring `
    --force

Write-Host ""
Write-Host "Scaffold completado. Revisa SIPV2.DataModels\Models\ y SIPV2.DataModels\Data\AppDbContext.cs" -ForegroundColor Green
