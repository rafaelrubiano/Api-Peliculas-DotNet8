# Agent Guidance for ApiPeliculas

## Project basics
- **Solution**: `ApiPeliculas.sln` (single project at `ApiPeliculas/ApiPeliculas.csproj`)
- **Target framework**: .NET 8 (`net8.0`)
- **SDK policy**: `global.json` pins `8.0.0` with `rollForward: latestMajor` and `allowPrerelease: true`
- **Nullable is disabled** (`<Nullable>disable</Nullable>`) — do not assume nullable reference types are enabled.
- **ImplicitUsings is enabled**.

## Running locally
- Use the `http` launch profile: `dotnet run --project ApiPeliculas --launch-profile http`
  - Opens Swagger at `http://localhost:5103/swagger`
- Alternatively: `dotnet run --project ApiPeliculas` (defaults to `http` profile)

## Database & EF Core
- Uses **EF Core 8.0.4 + SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer`)
- Uses **ASP.NET Core Identity** (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`)
- Connection string is **NOT hardcoded** in `appsettings.json` (uses User Secrets in development, environment variables in Docker/production).
- **Existing migrations** are present in `ApiPeliculas/Migrations/`. Ensure the SQL Server instance is reachable, then apply migrations or create the database:
  ```bash
  dotnet ef database update --project ApiPeliculas
  ```
- To add new migrations:
  ```bash
  dotnet ef migrations add <Nombre> --project ApiPeliculas
  ```

## Architecture & conventions
- **API versioning** is active (`Asp.Versioning.Mvc`):
  - Default version: `1.0`
  - Controllers are grouped under `Controllers/V1/` and `Controllers/V2/`
  - Swagger exposes two docs: `v1` and `v2`
- **AutoMapper** is configured with `PeliculasMapper` profile.
- **JWT Bearer authentication** is wired up in `Program.cs` using a symmetric key from `ApiSettings:Secreta`.
- **Response caching** is enabled globally with a `PorDefecto30Segundos` cache profile (30 seconds).
- **CORS** is restrictive: only `http://localhost:5103` is allowed by the `PoliticaCors` policy.
- **Static files** are served from `wwwroot`; images are stored under `wwwroot/ImagenesPeliculas/`.
- **Test project** exists at `ApiPeliculas.Tests/` with **xUnit + Moq** (13 tests for controllers).
- **Structured logging** with **Serilog** is implemented for cloud-ready observability (JSON format in production, readable in development).
- **ILogger<T>** is injected in all controllers with operational logging (Info/Warning/Error levels).

## Docker Compose (Desarrollo)

### Requisitos
- Docker Desktop o Docker Engine
- Docker Compose v2+

### Iniciar entorno de desarrollo
```bash
# Iniciar SQL Server + API
docker compose up -d

# Verificar logs
docker compose logs -f apipeliculas
docker compose logs -f sqlserver

# Detener
docker compose down

# Detener y eliminar volumen de datos
docker compose down -v
```

### Acceder a la aplicacion
- **API Swagger:** http://localhost:5103/swagger
- **SQL Server:** localhost:1433 (usuario: sa, password: r34llyStr0ngPwd123)

### Aplicar migraciones en Docker
```bash
# Ejecutar migraciones desde el contenedor
docker compose exec apipeliculas dotnet ef database update --project ApiPeliculas
```

### Variables de entorno
El archivo `.env.example` contiene las variables necesarias. Copiar a `.env` y ajustar:
```bash
cp .env.example .env
```

## Docker
- A `Dockerfile` exists at the **repo root** and builds the `ApiPeliculas` project.
- `compose.yaml` references the root `Dockerfile` correctly.
- The `Dockerfile` exposes ports `8080` and `8081`, and publishes to `/app/publish`.
- For development, use `docker compose up -d` (see Docker Compose section below).

## Secrets & Configuration

### User Secrets (Desarrollo Local)
El proyecto utiliza **ASP.NET Core User Secrets** para gestionar secrets en desarrollo. Los valores sensibles (JWT secret, connection string) NO están en `appsettings.json`.

**User Secrets ID:** `8610083b-338f-4fb8-bb20-b8aba676c31b`

**Secrets configurados:**
- `ApiSettings:Secreta` — JWT signing key
- `ConnectionStrings:ConexionSql` — SQL Server connection string

**Comandos útiles:**
```bash
# Ver secrets actuales
dotnet user-secrets list --project ApiPeliculas/ApiPeliculas.csproj

# Agregar/actualizar un secret
dotnet user-secrets set "ApiSettings:Secreta" "valor-secreto" --project ApiPeliculas/ApiPeliculas.csproj
dotnet user-secrets set "ConnectionStrings:ConexionSql" "Server=..." --project ApiPeliculas/ApiPeliculas.csproj

# Eliminar un secret
dotnet user-secrets remove "ApiSettings:Secreta" --project ApiPeliculas/ApiPeliculas.csproj

# Limpiar todos los secrets
dotnet user-secrets clear --project ApiPeliculas/ApiPeliculas.csproj
```

**Jerarquía de configuración (de menor a mayor prioridad):**
1. `appsettings.json` (valores vacíos/placeholders)
2. `appsettings.Development.json` (opcional)
3. **User Secrets** ← Usado en desarrollo
4. Variables de entorno ← Usado en producción/Docker
5. Argumentos de CLI

### Variables de Entorno (Producción/Docker)
En producción o Docker, usar variables de entorno:
```bash
# Linux/macOS
export ApiSettings__Secreta="valor-secreto"
export ConnectionStrings__ConexionSql="Server=..."

# Windows PowerShell
$env:ApiSettings__Secreta="valor-secreto"
$env:ConnectionStrings__ConexionSql="Server=..."
```

### Docker Compose
Para Docker Compose, agregar en `compose.yaml`:
```yaml
services:
  apipeliculas:
    environment:
      - ApiSettings__Secreta=${API_SECRET}
      - ConnectionStrings__ConexionSql=${DB_CONNECTION_STRING}
```

## Important constraints
- Do not enable nullable reference types project-wide without updating the `.csproj`.
- Do not change the `ApiSettings:Secreta` or connection string in `appsettings.json` unless intentionally replacing local secrets.
- If adding a new API version, register it in `Program.cs` under `SwaggerGen` and place controllers in the corresponding `Controllers/VX/` folder.
