# Components - ApiPeliculas

> **Last updated**: 2026-06-10
> **Dependencies**: .NET 8, EF Core 8.0.4, SQL Server, ASP.NET Core Identity

## NuGet Packages

| Package | Version | Purpose | Layer |
|---------|---------|---------|-------|
| `Microsoft.NET.Sdk.Web` | 8.0 | Web API framework | API |
| `Asp.Versioning.Mvc` | 8.0.0 | API versioning (URL-based) | API |
| `Asp.Versioning.Mvc.ApiExplorer` | 8.0.0 | Swagger integration for versioning | API |
| `AutoMapper` | 13.0.1 | Object-to-object mapping | API |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 | JWT authentication | Infrastructure |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.0.0 | Identity + EF Core integration | Infrastructure |
| `Microsoft.EntityFrameworkCore` | 8.0.4 | ORM | Infrastructure |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.4 | SQL Server provider | Infrastructure |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.4 | Migrations CLI | Infrastructure |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 8.0.7 | Scaffolding | Dev tool |
| `Swashbuckle.AspNetCore` | 6.6.2 | Swagger/OpenAPI documentation | API |

## Core Components

### 1. Database Context

**File**: `Data/ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext : IdentityDbContext<AppUsuario>
{
    public DbSet<Categoria> Categoria { get; set; }
    public DbSet<Pelicula> Pelicula { get; set; }
    public DbSet<Usuario> Usuario { get; set; }
    public DbSet<AppUsuario> AppUsuario { get; set; }
}
```

**Connection**: SQL Server localhost, `ApiPeliculasNET8` database, SQL auth (`sa` / `r34llyStr0ngPwd123`).

### 2. AutoMapper Profile

**File**: `PeliculasMappers/PeliculasMapper.cs`

Maps:
- `Categoria ↔ CategoriaDto`
- `Categoria ↔ CrearCategoriaDto`
- `Pelicula ↔ PeliculaDto`
- `Pelicula ↔ CrearPeliculaDto`
- `Pelicula ↔ ActualizarPeliculaDto`
- `Usuario ↔ UsuarioDto`
- `AppUsuario ↔ UsuarioDatosDto`
- `AppUsuario ↔ UsuarioDto`

### 3. JWT Authentication

**Configuration** (Program.cs):
- Symmetric key from `ApiSettings:Secreta`
- `ValidateIssuerSigningKey = true`
- `ValidateIssuer = false`
- `ValidateAudience = false`
- `RequireHttpsMetadata = false`

**Token generation**: Custom implementation in `UsuarioRepositorio` using `System.IdentityModel.Tokens.Jwt`.

### 4. Response Wrapper

**File**: `Modelos/RespuestaAPI.cs`

```csharp
public class RespuestaAPI
{
    public HttpStatusCode StatusCode { get; set; }
    public bool IsSuccess { get; set; } = true;
    public List<string> ErrorMessages { get; set; }
    public object Result { get; set; }
}
```

Used primarily in `UsuariosController` for `registro` and `login` endpoints.

### 5. File Upload Component

**Location**: `PeliculasController.cs` (inline, not a separate service)

**Logic**:
- Accepts `IFormFile` via `[FromForm]`
- Generates filename: `{pelicula.Id}{Guid}{extension}`
- Saves to: `wwwroot/ImagenesPeliculas/`
- Constructs URL: `{scheme}://{host}/ImagenesPeliculas/{filename}`
- Fallback: `https://placehold.co/600x400` if no image uploaded

**No validation** of file type, size, or content.

### 6. Repository Implementations

**CategoriaRepositorio**:
- Synchronous methods (`ICollection<T>`, `bool` returns)
- `Guardar()` returns `SaveChanges() >= 0`
- `ActualizarCategoria` uses `CurrentValues.SetValues` workaround for tracking issues
- `CrearCategoria` sets `FechaCreacion = DateTime.Now` (should be `DateTime.UtcNow`)

**PeliculaRepositorio**:
- Paginated query: `GetPeliculas(int pageNumber, int pageSize)`
- `GetTotalPeliculas()` for pagination metadata
- `GetPeliculasEnCategoria(int categoriaId)`
- `BuscarPelicula(string nombre)`

**UsuarioRepositorio**:
- `Registro` creates `AppUsuario` via `UserManager`
- `Login` validates via `SignInManager`
- `IsUniqueUser` checks username availability
- `GetUsuario` fetches by string ID (IdentityUser.Id)

### 7. Cache Configuration

**Global cache profile**: `PorDefecto30Segundos` (30 seconds)

Applied to:
- `GET /api/v1.0/categorias`
- `GET /api/v1.0/categorias/{id}`
- `GET /api/v1.0/usuarios/{id}`

### 8. CORS Policy

**Name**: `PoliticaCors`

**Configuration**:
- Allowed origin: `http://localhost:5103`
- Methods: `AllowAnyMethod`
- Headers: `AllowAnyHeader`
- No credentials restriction

### 9. API Versioning

**Default version**: 1.0

**URL format**: `api/v{version:apiVersion}/{resource}`

**Supported versions**:
- v1.0: Full CRUD for categorías, películas, usuarios
- v2.0: Demo endpoint (`GET /api/v2.0/categorias` returns string array)

**Swagger docs**: Two separate documents (`v1`, `v2`).

## Missing Components (Not Implemented)

- ❌ **MediatR** / CQRS
- ❌ **FluentValidation**
- ❌ **Result Pattern** (except manual `RespuestaAPI`)
- ❌ **Unit of Work** pattern
- ❌ **Global exception middleware**
- ❌ **Rate limiting**
- ❌ **Health checks**
- ❌ **Logging framework** (only basic ASP.NET logging)
- ❌ **Distributed cache** (Redis)
- ❌ **Background jobs** (Hangfire/Quartz)
- ❌ **Unit tests** / Integration tests
- ❌ **API documentation** beyond Swagger
