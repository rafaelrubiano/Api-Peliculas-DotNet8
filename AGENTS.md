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
- Connection string is hardcoded in `appsettings.json` pointing to `localhost` with a hardcoded SQL auth password (`sa` / `r34llyStr0ngPwd123`).
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
- There are **no test projects** in the repo.

## Docker
- A `Dockerfile` exists at the **repo root**, but `compose.yaml` references `ApiPeliculas/Dockerfile`, which **does not exist**.
  - If running Docker Compose, either move/copy the root `Dockerfile` or update `compose.yaml` to point to the root `Dockerfile`.
- The root `Dockerfile` builds the `ApiPeliculas` project, exposes ports `8080` and `8081`, and publishes to `/app/publish`.

## Important constraints
- Do not enable nullable reference types project-wide without updating the `.csproj`.
- Do not change the `ApiSettings:Secreta` or connection string in `appsettings.json` unless intentionally replacing local secrets.
- If adding a new API version, register it in `Program.cs` under `SwaggerGen` and place controllers in the corresponding `Controllers/VX/` folder.
