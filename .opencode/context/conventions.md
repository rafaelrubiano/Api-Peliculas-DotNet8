# Conventions - ApiPeliculas

> **Last updated**: 2026-06-10
> **Project**: ApiPeliculas (.NET 8)
> **Language**: Spanish (code comments, DTO names, error messages)

## Code Conventions

### Language & Naming

- **Code comments**: Spanish
- **Variable names**: Spanish (`listaCategorias`, `categoriaExistente`, `peliculaId`)
- **DTO names**: Spanish (`CrearCategoriaDto`, `PeliculaDto`, `UsuarioRegistroDto`)
- **Error messages**: Spanish ("La categoría ya existe!", "No se encontró la película")
- **Method names**: Spanish ( `GetCategorias`, `CrearCategoria`, `ActualizarPatchCategoria`)
- **Controller names**: English pluralized (`CategoriasController`, `PeliculasController`, `UsuariosController`)
- **Repository names**: English (`CategoriaRepositorio`, `PeliculaRepositorio`)
- **Interface names**: English (`ICategoriaRepositorio`, `IPeliculaRepositorio`)

### .NET Conventions

- **Nullable**: `<Nullable>disable</Nullable>` (disabled project-wide)
- **Implicit usings**: `<ImplicitUsings>enable</ImplicitUsings>`
- **Target framework**: `net8.0`
- **Docker target**: `Linux`

### File Organization

```
Controllers/          → Grouped by API version (V1/, V2/)
Modelos/              → Entities + DTOs (mixed)
Repositorio/          → Implementations
Repositorio/IRepositorio/ → Interfaces (separate subfolder)
Data/                 → DbContext only
PeliculasMappers/     → Single AutoMapper profile
Migrations/           → EF Core migrations (timestamped)
wwwroot/              → Static files (images)
```

## API Conventions

### Routing

- **Pattern**: `api/v{version:apiVersion}/{resource}`
- **Version in URL**: Yes (e.g., `/api/v1.0/categorias`)
- **Version in header**: No (commented out in code)
- **Version in query string**: No (commented out in code)

### HTTP Methods

| Method | Usage |
|--------|-------|
| GET | Retrieve resources (list, single, search) |
| POST | Create resources (categorías, películas, registro) |
| PUT | Full update (categorías only) |
| PATCH | Partial update (used synonymously with PUT) |
| DELETE | Remove resources |

### Response Patterns

**Success (GET list)**:
```json
[
  { "id": 1, "nombre": "..." }
]
```

**Success (GET single)**:
```json
{ "id": 1, "nombre": "..." }
```

**Success (POST)**:
```json
{ "id": 1, "nombre": "..." }
```

**Success (Paginated)**:
```json
{
  "pageNumber": 1,
  "pageSize": 2,
  "totalPages": 5,
  "totalItems": 10,
  "items": [...]
}
```

**Error (Validation)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Nombre": ["El nombre es obligatorio"]
  }
}
```

**Error (Business - UsuariosController)**:
```json
{
  "statusCode": 400,
  "isSuccess": false,
  "errorMessages": ["El nombre de usuario ya existe"],
  "result": null
}
```

**Error (Unhandled)**:
```json
"Error recuperando datos de la aplicaión"
```

### Status Codes

| Code | Used For | Consistent |
|------|----------|------------|
| 200 | GET success | ✅ Yes |
| 201 | POST success | ✅ Yes |
| 204 | PUT/PATCH/DELETE success | ✅ Yes |
| 400 | Bad request, validation | ⚠️ Mixed (sometimes 404 used) |
| 401 | Unauthorized | ✅ Yes |
| 403 | Forbidden | ✅ Documented but rarely used |
| 404 | Not found | ⚠️ Sometimes used for validation errors |
| 500 | Server error | ✅ Yes |

### Authentication

- **Scheme**: Bearer JWT
- **Header**: `Authorization: Bearer {token}`
- **Token generation**: Custom (in `UsuarioRepositorio`)
- **Token validation**: Symmetric key, no issuer/audience validation
- **Roles**: `Admin`, `User`

## Database Conventions

### Table Naming

- **Pluralized**: `Categorias`, `Peliculas`, `Usuarios`
- **Singular in code**: `Categoria`, `Pelicula`, `Usuario` (entity classes)
- **Migration names**: Spanish descriptive (`MigracionInicial`, `CrearTablaPelicula`, `AgregadoSoporteIdentity`)

### Column Naming

- **Camel case in code**: `categoriaId`, `rutaImagen`, `fechaCreacion`
- **Pascal case in DTOs**: `CategoriaId`, `RutaImagen`, `FechaCreacion`

### Key Conventions

- **Primary keys**: `Id` (int, auto-increment)
- **Foreign keys**: `{Entity}Id` (e.g., `categoriaId`)
- **Identity keys**: String (GUID) for `AppUsuario`

## EF Core Conventions

### DbContext

- **Inherits from**: `IdentityDbContext<AppUsuario>`
- **DbSet naming**: Singular (`Categoria`, `Pelicula`, `Usuario`)
- **Fluent configuration**: Minimal (only `base.OnModelCreating` called)

### Migrations

- **Naming**: `{Timestamp}_{DescripcionEnEspanol}`
- **Generated**: Via `dotnet ef migrations add`
- **Apply**: `dotnet ef database update --project ApiPeliculas`

## Validation Conventions

### Data Annotations

```csharp
[Key]                    // Primary key
[Required]               // Not null
[MaxLength(100)]         // String length
[ForeignKey("id")]       // Foreign key relationship
```

### ModelState

- Checked manually in controllers: `if (!ModelState.IsValid)`
- Returns `BadRequest(ModelState)` on failure
- Sometimes adds custom errors: `ModelState.AddModelError("", "message")`

## File Upload Conventions

### Image Handling

- **Storage**: Local filesystem (`wwwroot/ImagenesPeliculas/`)
- **Naming**: `{pelicula.Id}{Guid}{extension}`
- **URL construction**: `{scheme}://{host}/ImagenesPeliculas/{filename}`
- **Fallback**: `https://placehold.co/600x400`
- **No validation**: File type, size, or content not validated

## Git Conventions

### Commit Messages (Inferred)

- Spanish descriptive messages
- Examples: `feat: agregar endpoint de películas`, `fix: corregir validación de email`
- No strict conventional commits format observed

## Development Conventions

### Running Locally

```bash
# Preferred command
dotnet run --project ApiPeliculas --launch-profile http

# Swagger URL
http://localhost:5103/swagger
```

### Database Updates

```bash
# Apply migrations
dotnet ef database update --project ApiPeliculas

# Create new migration
dotnet ef migrations add <Nombre> --project ApiPeliculas
```

### Docker

- **Dockerfile location**: Root of repo
- **Compose file**: `compose.yaml` (references incorrect path)
- **Issue**: `compose.yaml` points to `ApiPeliculas/Dockerfile` which does not exist

## Swagger Conventions

### Documentation

- **Title**: "Peliculas Api V1" / "Peliculas Api V2"
- **Description**: "Api de Peliculas Versión X"
- **Contact**: "Codex-io" (placeholder URL)
- **License**: "Desarrollo de Software" (placeholder URL)
- **Terms of Service**: `https://google.com` (placeholder)

### Security

- Bearer token input in Swagger UI
- Global security requirement applied

## Inconsistencies to Note

1. **Mixed naming**: Some files use Spanish (`Repositorio`), some English (`Controllers`)
2. **Mixed async/sync**: `Registro` and `Login` are async, but repository methods are sync
3. **Mixed status codes**: 404 used for validation errors (should be 400)
4. **Mixed DTOs**: `CrearCategoriaDto` lacks `Id`, but `CategoriaDto` has it; `ActualizarPeliculaDto` has `Id` (different from create pattern)
5. **Mixed authorization**: Some endpoints have `[Authorize]` commented out
6. **Commented code**: Extensive blocks of commented code in controllers (legacy endpoints)

## Recommended Clean Conventions

When migrating to Clean Architecture:

- **English naming** for all code (classes, methods, variables)
- **Separate projects** by layer (Domain, Application, Infrastructure, API)
- **CQRS** with MediatR (Commands for writes, Queries for reads)
- **Result Pattern** for consistent error handling
- **FluentValidation** for validation rules
- **Global middleware** for exceptions and logging
- **Async all the way** (`Task<T>` in repositories, services, controllers)
- **CancellationToken** in all async methods
- **UTC timestamps** (`DateTime.UtcNow` instead of `DateTime.Now`)
- **Value Objects** for complex types (Clasificacion, Email, etc.)
- **Domain Events** for side effects
- **Specification Pattern** for complex queries
