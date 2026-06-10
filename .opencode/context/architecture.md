# Architecture - ApiPeliculas

> **Last updated**: 2026-06-10
> **Current pattern**: Monolithic N-Layer (single project)
> **Target pattern**: Clean Architecture (Domain → Application → Infrastructure → API)

## Current Architecture

### Diagram (Simplified)

```
┌─────────────────────────────────────────┐
│           ApiPeliculas.csproj           │
│  ┌─────────────────────────────────┐    │
│  │  Controllers (V1, V2)          │    │
│  │  - CategoriasController        │    │
│  │  - PeliculasController          │    │
│  │  - UsuariosController          │    │
│  └─────────────────────────────────┘    │
│              │                          │
│              ▼                          │
│  ┌─────────────────────────────────┐    │
│  │  Repositories (I + Impl)        │    │
│  │  - CategoriaRepositorio        │    │
│  │  - PeliculaRepositorio          │    │
│  │  - UsuarioRepositorio          │    │
│  └─────────────────────────────────┘    │
│              │                          │
│              ▼                          │
│  ┌─────────────────────────────────┐    │
│  │  EF Core / Identity              │    │
│  │  - ApplicationDbContext          │    │
│  │  - UserManager / SignInManager  │    │
│  └─────────────────────────────────┘    │
│              │                          │
│              ▼                          │
│  ┌─────────────────────────────────┐    │
│  │  SQL Server                      │    │
│  └─────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

### Layer Analysis

**Presentation Layer (Implicit)**:
- Controllers: Handle HTTP, validation, response generation
- Problem: Controllers contain business logic (file upload, URL generation, validation)

**Business Logic Layer (Missing)**:
- No service layer between controllers and repositories
- Controllers directly call repositories

**Data Access Layer (Mixed)**:
- Repositories implement CRUD + some business logic
- EF Core configuration in `OnModelCreating` (minimal)
- DbContext mixed with Identity

## Architectural Decisions

### 1. Single Project

**Decision**: All code in one assembly.
**Reason**: Simplicity, educational project.
**Trade-off**: No separation of concerns, high coupling, difficult to test.

### 2. Repository Pattern

**Decision**: Interface + Implementation per entity.
**Pros**: Abstracts EF Core, enables unit testing (in theory).
**Cons**: Synchronous methods, no `IQueryable` return, no async/await in interfaces.

### 3. Synchronous Repositories

**Decision**: `ICollection<T>`, `bool`, `Categoria` return types (not `Task<T>`).
**Impact**: Blocks thread pool under load.
**Example**:
```csharp
public interface ICategoriaRepositorio {
    ICollection<Categoria> GetCategorias();  // Not async
    bool CrearCategoria(Categoria categoria); // Not async
}
```

### 4. Mixed DTOs and Entities

**Decision**: DTOs and Entities in same `Modelos/` folder.
**Problem**: No clear boundary. AutoMapper maps both directions.

### 5. Inline File Handling

**Decision**: File upload logic in `PeliculasController`.
**Problem**: Violates SRP, not reusable, not testable.

### 6. Custom JWT Implementation

**Decision**: Manual JWT generation in `UsuarioRepositorio`.
**Configuration**: Minimal validation (`ValidateIssuer=false`, `ValidateAudience=false`).
**Risk**: Tokens are less secure than standard Identity Server implementation.

### 7. Response Caching

**Decision**: Global cache profile `PorDefecto30Segundos`.
**Applied to**: Select GET endpoints.
**Missing**: Cache invalidation, cache variations, distributed cache.

## Coupling Analysis

### High Coupling

1. **Controllers ↔ EF Core**: Controllers know about `DbContext` indirectly via repositories.
2. **Controllers ↔ Identity**: `UsuariosController` uses `RespuestaAPI` which is tied to current implementation.
3. **Repositories ↔ DateTime.Now**: `CategoriaRepositorio` sets `FechaCreacion = DateTime.Now`.
4. **AutoMapper ↔ All Layers**: Single profile maps entities to DTOs and vice versa.

### Dependencies Point Outward

In Clean Architecture, dependencies should point inward. Here:
- `Controllers` depend on `Repositories` (correct direction)
- `Repositories` depend on `DbContext` (correct direction)
- But all in same project means no enforcement.

## SOLID Assessment

| Principle | Status | Notes |
|-----------|--------|-------|
| **SRP** | ❌ Violated | Controllers do too much; Repositories have business logic |
| **OCP** | ⚠️ Partial | Repositories can be extended, but controllers are rigid |
| **LSP** | ✅ Compliant | Interfaces allow substitution |
| **ISP** | ✅ Compliant | Separate interfaces per entity |
| **DIP** | ⚠️ Partial | Controllers depend on abstractions (interfaces), but concrete frameworks leak in |

## Data Flow

### GET /api/v1.0/categorias

```
HTTP Request
    ↓
[CategoriasController.GetCategorias]
    ↓
[CategoriaRepositorio.GetCategorias]
    ↓
[ApplicationDbContext.Categoria]
    ↓
[SQL Server]
    ↓
[Categoria entities]
    ↓
[AutoMapper.Map<CategoriaDto>]
    ↓
[JSON Response]
```

### POST /api/v1.0/peliculas

```
HTTP Request + FormData
    ↓
[PeliculasController.CrearPelicula]
    ↓
[ModelState validation]
    ↓
[Existence check: _pelRepo.ExistePelicula]
    ↓
[AutoMapper.Map<Pelicula>]
    ↓
[File upload logic (inline)]
    ↓
[PeliculaRepositorio.CrearPelicula]
    ↓
[ApplicationDbContext.Pelicula.Add]
    ↓
[SQL Server]
    ↓
[201 Created Response]
```

## Security Architecture

### Authentication Flow

```
[Client] --(POST /login)--> [UsuariosController]
    ↓
[UsuarioRepositorio.Login] --> [SignInManager.CheckPasswordSignInAsync]
    ↓
[UserManager.FindByNameAsync] --> [SQL Server]
    ↓
[Token generation] --(JWT + symmetric key)--> [Client]
```

### Authorization Flow

```
[Client] --(GET /admin-endpoint + Bearer token)--> [Controller]
    ↓
[JWT Middleware] --(Validate token)--> [Authorize filter]
    ↓
[Role check: "Admin"] --> [403 if not] / [Execute action if yes]
```

**Roles**: `Admin`, `User` (Identity roles).

**No claims-based authorization**: Only `[Authorize(Roles = "...")]`.

## Database Architecture

### Schema (Inferred)

```sql
-- Categoria
CREATE TABLE Categoria (
    Id INT PRIMARY KEY IDENTITY,
    Nombre NVARCHAR(MAX) NOT NULL,
    FechaCreacion DATETIME2 NOT NULL
);

-- Pelicula
CREATE TABLE Pelicula (
    Id INT PRIMARY KEY IDENTITY,
    Nombre NVARCHAR(MAX) NULL,
    Descripcion NVARCHAR(MAX) NULL,
    Duracion INT NOT NULL,
    RutaImagen NVARCHAR(MAX) NULL,
    RutaLocalImagen NVARCHAR(MAX) NULL,
    Clasificacion INT NOT NULL,
    FechaCreacion DATETIME2 NOT NULL,
    categoriaId INT NOT NULL,
    FOREIGN KEY (categoriaId) REFERENCES Categoria(Id)
);

-- Usuario (Identity)
-- ASP.NET Core Identity tables (AspNetUsers, AspNetRoles, etc.)
-- Custom: Usuario table (legacy, may coexist with Identity)
```

### Migrations History

1. `20241127185718_MigracionInicial` - Initial schema
2. `20241215011900_Cambio de plurar a singular` - Renamed table
3. `20241221151832_CrearTablaPelicula` - Added Pelicula table
4. `20241222203351_CreacionTablaUsuario` - Added Usuario table
5. `20241231011930_AgregadoSoporteIdentity` - Added Identity
6. `20241231203057_SoporteParaSubidaImagenPelicula` - Added image fields

## Target Architecture (Clean)

```
┌─────────────────────────────────────────┐
│         ApiPeliculas.API                │
│  - Controllers (thin)                   │
│  - Middleware                           │
│  - DI Configuration                     │
└─────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│      ApiPeliculas.Application           │
│  - MediatR Handlers (CQRS)              │
│  - DTOs                                 │
│  - Validators (FluentValidation)        │
│  - Behaviors (pipeline)                 │
└─────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│    ApiPeliculas.Infrastructure          │
│  - EF Core Repositories                 │
│  - Identity Configuration               │
│  - JWT Service                          │
│  - File Storage Service                 │
└─────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│      ApiPeliculas.Domain                │
│  - Entities (rich)                      │
│  - Interfaces (contracts)               │
│  - Exceptions                           │
│  - Value Objects                        │
└─────────────────────────────────────────┘
```

## Performance Considerations

### Current

- **N+1 queries**: `GetCategorias` returns all, then maps one by one.
- **No `AsNoTracking`**: Not used for read-only queries.
- **No `ProjectTo`**: Manual mapping with `Select` + `ToList`.
- **No `CancellationToken`**: Async methods don't accept cancellation.
- **Synchronous I/O**: File upload uses `CopyTo` (not `CopyToAsync`).

### Recommended

- Use `AsNoTracking()` for GET endpoints.
- Use `ProjectTo<PeliculaDto>` for EF Core projection.
- Accept `CancellationToken` in all async methods.
- Use `CopyToAsync` for file streams.
- Add Redis for distributed caching.

## Scalability Constraints

1. **Single project**: Cannot scale layers independently.
2. **Synchronous repos**: Thread pool exhaustion under load.
3. **Local file storage**: Not suitable for multi-instance deployment.
4. **No message queue**: All operations are synchronous.
5. **In-memory caching**: No shared cache across instances.

## Documentation References

- `.opencode/docs/architecture-analysis.md` - Detailed analysis
- `.opencode/docs/migration-plan.md` - Migration roadmap
