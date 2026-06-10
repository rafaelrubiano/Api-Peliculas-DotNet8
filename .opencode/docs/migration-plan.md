# Plan de Migración a Clean Architecture - ApiPeliculas

> **Fecha**: 2026-06-10
> **Origen**: Arquitectura monolítica N-Layer (single project)
> **Destino**: Clean Architecture (Domain → Application → Infrastructure → API)
> **Estrategia**: Migración incremental, feature-by-feature, sin romper funcionalidad existente

---

## 1. Estrategia de Migración

### Principios Guía

1. **Migración incremental**: No reescribir todo de golpe. Migrar feature por feature.
2. **Funcionalidad preservada**: Cada paso debe mantener la API 100% funcional.
3. **Commits atómicos**: Cada fase es un commit independiente con rollback posible.
4. **Backward compatibility**: Los endpoints v1 y v2 deben seguir respondiendo igual.
5. **Database primero**: Las migrations de EF Core se mantienen; no tocar la base de datos.

### Estado Inicial (Snapshot)

```
ApiPeliculas/
└── ApiPeliculas/ (single project)
    ├── Controllers/ (V1, V2, Usuarios)
    ├── Data/
    ├── Modelos/
    ├── Repositorio/
    ├── PeliculasMappers/
    ├── Migrations/
    ├── wwwroot/
    └── Program.cs
```

### Estado Final (Objetivo)

```
ApiPeliculas/
├── ApiPeliculas.Domain/           ← Entidades, interfaces, excepciones
│   ├── Entities/
│   ├── Interfaces/
│   ├── Exceptions/
│   └── ValueObjects/
├── ApiPeliculas.Application/      ← Casos de uso, DTOs, validaciones, CQRS
│   ├── Features/
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Behaviors/
│   └── Mappings/
├── ApiPeliculas.Infrastructure/  ← EF Core, Identity, FileStorage, JWT
│   ├── Persistence/
│   ├── Identity/
│   ├── Services/
│   └── Configuration/
├── ApiPeliculas.API/              ← Controllers, Middleware, Program.cs
│   ├── Controllers/ (V1, V2)
│   ├── Middleware/
│   └── Extensions/
└── ApiPeliculas.sln
```

---

## 2. Fase 0: Preparación (Día 0-1)

### 2.1 Pre-requisitos

- [ ] Backup de la base de datos actual
- [ ] Commit actual en `main` con tag `v1.0-legacy`
- [ ] Crear rama `feature/clean-architecture-migration`
- [ ] Verificar que la aplicación compila y corre: `dotnet run --project ApiPeliculas`
- [ ] Verificar Swagger en `http://localhost:5103/swagger`
- [ ] Ejecutar tests existentes (si los hay): `dotnet test`

### 2.2 Instalar herramientas adicionales

```bash
# Agregar al .csproj actual o instalar global
dotnet add package MediatR --version 12.2.0
dotnet add package FluentValidation --version 11.9.0
dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.9.0
dotnet add package Result --version 2.0.0  # O usar una implementación propia
```

### 2.3 Crear estructura de carpetas temporal

```bash
# Dentro de la carpeta del proyecto actual
cd ApiPeliculas
mkdir -p Domain/Entities Domain/Interfaces Domain/Exceptions Domain/ValueObjects
mkdir -p Application/Features Application/DTOs Application/Interfaces Application/Behaviors Application/Mappings
mkdir -p Infrastructure/Persistence Infrastructure/Identity Infrastructure/Services Infrastructure/Configuration
```

> **Nota**: En esta fase, la aplicación sigue funcionando exactamente igual. Solo estamos organizando carpetas.

---

## 3. Fase 1: Capa de Dominio (Domain Layer) (Día 1-2)

### Objetivo
Crear entidades ricas con comportamiento, sin dependencias de frameworks.

### 3.1 Crear proyecto Domain

```bash
# Desde la raíz del solution
dotnet new classlib -n ApiPeliculas.Domain -o ApiPeliculas.Domain
dotnet sln add ApiPeliculas.Domain/ApiPeliculas.Domain.csproj
```

### 3.2 Mover entidades actuales a Domain

**Archivo**: `ApiPeliculas.Domain/Entities/Categoria.cs`

```csharp
namespace ApiPeliculas.Domain.Entities;

public class Categoria
{
    public int Id { get; private set; }
    public string Nombre { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    
    // Navigation property
    public ICollection<Pelicula> Peliculas { get; private set; } = new List<Pelicula>();

    // EF Core constructor
    private Categoria() { }

    public Categoria(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre de la categoría es obligatorio");
            
        if (nombre.Length > 100)
            throw new DomainException("El nombre no puede exceder 100 caracteres");

        Nombre = nombre;
        FechaCreacion = DateTime.UtcNow;
    }

    public void Renombrar(string nuevoNombre)
    {
        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new DomainException("El nombre no puede estar vacío");
            
        if (nuevoNombre.Length > 100)
            throw new DomainException("El nombre no puede exceder 100 caracteres");

        Nombre = nuevoNombre;
    }
}
```

**Archivo**: `ApiPeliculas.Domain/Entities/Pelicula.cs`

```csharp
namespace ApiPeliculas.Domain.Entities;

public class Pelicula
{
    public int Id { get; private set; }
    public string Nombre { get; private set; }
    public string Descripcion { get; private set; }
    public int Duracion { get; private set; }
    public string? RutaImagen { get; private set; }
    public string? RutaLocalImagen { get; private set; }
    public ClasificacionPelicula Clasificacion { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    
    // Relación
    public int CategoriaId { get; private set; }
    public Categoria Categoria { get; private set; }

    private Pelicula() { }

    public Pelicula(string nombre, string descripcion, int duracion, int categoriaId, ClasificacionPelicula clasificacion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre es obligatorio");
        if (duracion <= 0)
            throw new DomainException("La duración debe ser mayor a 0");
        if (categoriaId <= 0)
            throw new DomainException("La categoría es obligatoria");

        Nombre = nombre;
        Descripcion = descripcion;
        Duracion = duracion;
        CategoriaId = categoriaId;
        Clasificacion = clasificacion;
        FechaCreacion = DateTime.UtcNow;
    }

    public void AsignarImagen(string rutaImagen, string rutaLocalImagen)
    {
        RutaImagen = rutaImagen;
        RutaLocalImagen = rutaLocalImagen;
    }

    public void Actualizar(string nombre, string descripcion, int duracion, int categoriaId, ClasificacionPelicula clasificacion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre es obligatorio");
        if (duracion <= 0)
            throw new DomainException("La duración debe ser mayor a 0");

        Nombre = nombre;
        Descripcion = descripcion;
        Duracion = duracion;
        CategoriaId = categoriaId;
        Clasificacion = clasificacion;
    }
}

public enum ClasificacionPelicula
{
    Siete = 7,
    Trece = 13,
    Diesciseis = 16,
    Diesciocho = 18
}
```

**Archivo**: `ApiPeliculas.Domain/Entities/Usuario.cs`

```csharp
namespace ApiPeliculas.Domain.Entities;

public class Usuario
{
    public string Id { get; private set; }
    public string NombreUsuario { get; private set; }
    public string Nombre { get; private set; }
    public string Role { get; private set; }

    private Usuario() { }

    public Usuario(string id, string nombreUsuario, string nombre, string role)
    {
        Id = id;
        NombreUsuario = nombreUsuario;
        Nombre = nombre;
        Role = role;
    }
}
```

### 3.2 Crear excepciones de dominio

**Archivo**: `ApiPeliculas.Domain/Exceptions/DomainException.cs`

```csharp
namespace ApiPeliculas.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key) 
        : base($"{entityName} con id {key} no fue encontrado") { }
}

public class ValidationException : DomainException
{
    public List<string> Errors { get; } = new();
    
    public ValidationException(string message) : base(message) { }
    public ValidationException(IEnumerable<string> errors) : base("Se encontraron errores de validación")
    {
        Errors.AddRange(errors);
    }
}
```

### 3.3 Crear interfaces de repositorio (contratos)

**Archivo**: `ApiPeliculas.Domain/Interfaces/ICategoriaRepository.cs`

```csharp
using ApiPeliculas.Domain.Entities;

namespace ApiPeliculas.Domain.Interfaces;

public interface ICategoriaRepository
{
    Task<IEnumerable<Categoria>> GetAllAsync();
    Task<Categoria?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string nombre);
    Task<bool> ExistsByIdAsync(int id);
    Task AddAsync(Categoria categoria);
    Task UpdateAsync(Categoria categoria);
    Task DeleteAsync(Categoria categoria);
    Task<int> SaveChangesAsync();
}
```

**Archivo**: `ApiPeliculas.Domain/Interfaces/IPeliculaRepository.cs`

```csharp
using ApiPeliculas.Domain.Entities;

namespace ApiPeliculas.Domain.Interfaces;

public interface IPeliculaRepository
{
    Task<(IEnumerable<Pelicula> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize);
    Task<Pelicula?> GetByIdAsync(int id);
    Task<IEnumerable<Pelicula>> GetByCategoriaAsync(int categoriaId);
    Task<IEnumerable<Pelicula>> SearchAsync(string nombre);
    Task<bool> ExistsByNameAsync(string nombre);
    Task<bool> ExistsByIdAsync(int id);
    Task AddAsync(Pelicula pelicula);
    Task UpdateAsync(Pelicula pelicula);
    Task DeleteAsync(Pelicula pelicula);
    Task<int> SaveChangesAsync();
}
```

**Archivo**: `ApiPeliculas.Domain/Interfaces/IUsuarioRepository.cs`

```csharp
using ApiPeliculas.Domain.Entities;

namespace ApiPeliculas.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(string id);
    Task<bool> IsUsernameUniqueAsync(string username);
    Task<(Usuario? Usuario, string? Token, string? Role)> RegisterAsync(string username, string nombre, string password, string role);
    Task<(Usuario? Usuario, string? Token, string? Role)> LoginAsync(string username, string password);
}
```

### 3.4 Crear interfaces de servicios externos

**Archivo**: `ApiPeliculas.Domain/Interfaces/IFileStorageService.cs`

```csharp
namespace ApiPeliculas.Domain.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteFileAsync(string filePath);
    string GetFileUrl(string fileName);
    bool IsValidFileType(string contentType);
    bool IsValidFileSize(long fileSize);
}
```

**Archivo**: `ApiPeliculas.Domain/Interfaces/IJwtService.cs`

```csharp
namespace ApiPeliculas.Domain.Interfaces;

public interface IJwtService
{
    string GenerateToken(string userId, string username, string role);
    bool ValidateToken(string token);
}
```

### 3.5 Resultado de esta fase

```
ApiPeliculas.Domain/
├── ApiPeliculas.Domain.csproj
├── Entities/
│   ├── Categoria.cs
│   ├── Pelicula.cs
│   ├── Usuario.cs
│   └── ClasificacionPelicula.cs
├── Interfaces/
│   ├── ICategoriaRepository.cs
│   ├── IPeliculaRepository.cs
│   ├── IUsuarioRepository.cs
│   ├── IFileStorageService.cs
│   └── IJwtService.cs
└── Exceptions/
    ├── DomainException.cs
    ├── NotFoundException.cs
    └── ValidationException.cs
```

**Validación**: Compilar solo el proyecto Domain:
```bash
dotnet build ApiPeliculas.Domain/ApiPeliculas.Domain.csproj
```

Debe compilar sin errores y **sin dependencias de frameworks externos**.

---

## 4. Fase 2: Capa de Aplicación (Application Layer) (Día 2-4)

### Objetivo
Crear casos de uso con CQRS + MediatR, DTOs, validación, y mapeo.

### 4.1 Crear proyecto Application

```bash
dotnet new classlib -n ApiPeliculas.Application -o ApiPeliculas.Application
dotnet sln add ApiPeliculas.Application/ApiPeliculas.Application.csproj
```

**Agregar referencias**:
```bash
dotnet add ApiPeliculas.Application/ApiPeliculas.Application.csproj reference ApiPeliculas.Domain/ApiPeliculas.Domain.csproj
```

**Agregar paquetes**:
```bash
dotnet add ApiPeliculas.Application/ApiPeliculas.Application.csproj package MediatR
```

### 4.2 DTOs de aplicación

**Archivo**: `ApiPeliculas.Application/DTOs/CategoriaDTO.cs`

```csharp
namespace ApiPeliculas.Application.DTOs;

public class CategoriaDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class CrearCategoriaDTO
{
    public string Nombre { get; set; } = string.Empty;
}

public class ActualizarCategoriaDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
```

**Archivo**: `ApiPeliculas.Application/DTOs/PeliculaDTO.cs`

```csharp
namespace ApiPeliculas.Application.DTOs;

public class PeliculaDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Duracion { get; set; }
    public string? RutaImagen { get; set; }
    public string? RutaLocalImagen { get; set; }
    public string Clasificacion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public int CategoriaId { get; set; }
}

public class CrearPeliculaDTO
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Duracion { get; set; }
    public int CategoriaId { get; set; }
    public string Clasificacion { get; set; } = string.Empty;
    public Stream? ImagenStream { get; set; }
    public string? ImagenFileName { get; set; }
    public string? ImagenContentType { get; set; }
}

public class ActualizarPeliculaDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Duracion { get; set; }
    public int CategoriaId { get; set; }
    public string Clasificacion { get; set; } = string.Empty;
    public Stream? ImagenStream { get; set; }
    public string? ImagenFileName { get; set; }
    public string? ImagenContentType { get; set; }
}

public class PaginatedList<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public List<T> Items { get; set; } = new();
}
```

**Archivo**: `ApiPeliculas.Application/DTOs/UsuarioDTO.cs`

```csharp
namespace ApiPeliculas.Application.DTOs;

public class UsuarioDTO
{
    public string Id { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UsuarioRegistroDTO
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UsuarioLoginDTO
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UsuarioLoginRespuestaDTO
{
    public UsuarioDTO Usuario { get; set; } = new();
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
```

### 4.3 Implementar Result Pattern

**Archivo**: `ApiPeliculas.Application/Common/Result.cs`

```csharp
namespace ApiPeliculas.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string Error { get; }
    public List<string> Errors { get; } = new();

    private Result(bool isSuccess, T? value, string error, List<string>? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        if (errors != null) Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty, null);
    public static Result<T> Failure(string error) => new(false, default, error, null);
    public static Result<T> Failure(List<string> errors) => new(false, default, string.Join(", ", errors), errors);
}

public class Result
{
    public bool IsSuccess { get; }
    public string Error { get; }
    public List<string> Errors { get; } = new();

    private Result(bool isSuccess, string error, List<string>? errors)
    {
        IsSuccess = isSuccess;
        Error = error;
        if (errors != null) Errors = errors;
    }

    public static Result Success() => new(true, string.Empty, null);
    public static Result Failure(string error) => new(false, error, null);
    public static Result Failure(List<string> errors) => new(false, string.Join(", ", errors), errors);
}
```

### 4.4 Casos de uso con MediatR (CQRS)

#### Comandos

**Archivo**: `ApiPeliculas.Application/Features/Categorias/Commands/CrearCategoriaCommand.cs`

```csharp
using ApiPeliculas.Application.Common;
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Domain.Entities;
using ApiPeliculas.Domain.Interfaces;
using MediatR;

namespace ApiPeliculas.Application.Features.Categorias.Commands;

public record CrearCategoriaCommand(string Nombre) : IRequest<Result<CategoriaDTO>>;

public class CrearCategoriaCommandHandler : IRequestHandler<CrearCategoriaCommand, Result<CategoriaDTO>>
{
    private readonly ICategoriaRepository _repository;

    public CrearCategoriaCommandHandler(ICategoriaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CategoriaDTO>> Handle(CrearCategoriaCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByNameAsync(request.Nombre))
            return Result<CategoriaDTO>.Failure("La categoría ya existe");

        var categoria = new Categoria(request.Nombre);
        await _repository.AddAsync(categoria);
        await _repository.SaveChangesAsync();

        return Result<CategoriaDTO>.Success(new CategoriaDTO
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            FechaCreacion = categoria.FechaCreacion
        });
    }
}
```

**Archivo**: `ApiPeliculas.Application/Features/Categorias/Commands/ActualizarCategoriaCommand.cs`

```csharp
using ApiPeliculas.Application.Common;
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Domain.Exceptions;
using ApiPeliculas.Domain.Interfaces;
using MediatR;

namespace ApiPeliculas.Application.Features.Categorias.Commands;

public record ActualizarCategoriaCommand(int Id, string Nombre) : IRequest<Result<CategoriaDTO>>;

public class ActualizarCategoriaCommandHandler : IRequestHandler<ActualizarCategoriaCommand, Result<CategoriaDTO>>
{
    private readonly ICategoriaRepository _repository;

    public ActualizarCategoriaCommandHandler(ICategoriaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CategoriaDTO>> Handle(ActualizarCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await _repository.GetByIdAsync(request.Id);
        if (categoria == null)
            return Result<CategoriaDTO>.Failure($"No se encontró la categoría con ID {request.Id}");

        categoria.Renombrar(request.Nombre);
        await _repository.UpdateAsync(categoria);
        await _repository.SaveChangesAsync();

        return Result<CategoriaDTO>.Success(new CategoriaDTO
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            FechaCreacion = categoria.FechaCreacion
        });
    }
}
```

**Archivo**: `ApiPeliculas.Application/Features/Categorias/Commands/EliminarCategoriaCommand.cs`

```csharp
using ApiPeliculas.Application.Common;
using ApiPeliculas.Domain.Interfaces;
using MediatR;

namespace ApiPeliculas.Application.Features.Categorias.Commands;

public record EliminarCategoriaCommand(int Id) : IRequest<Result>;

public class EliminarCategoriaCommandHandler : IRequestHandler<EliminarCategoriaCommand, Result>
{
    private readonly ICategoriaRepository _repository;

    public EliminarCategoriaCommandHandler(ICategoriaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(EliminarCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await _repository.GetByIdAsync(request.Id);
        if (categoria == null)
            return Result.Failure($"No se encontró la categoría con ID {request.Id}");

        await _repository.DeleteAsync(categoria);
        await _repository.SaveChangesAsync();

        return Result.Success();
    }
}
```

#### Queries

**Archivo**: `ApiPeliculas.Application/Features/Categorias/Queries/GetCategoriasQuery.cs`

```csharp
using ApiPeliculas.Application.Common;
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Domain.Interfaces;
using MediatR;

namespace ApiPeliculas.Application.Features.Categorias.Queries;

public record GetCategoriasQuery : IRequest<Result<IEnumerable<CategoriaDTO>>>;

public class GetCategoriasQueryHandler : IRequestHandler<GetCategoriasQuery, Result<IEnumerable<CategoriaDTO>>>
{
    private readonly ICategoriaRepository _repository;

    public GetCategoriasQueryHandler(ICategoriaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<CategoriaDTO>>> Handle(GetCategoriasQuery request, CancellationToken cancellationToken)
    {
        var categorias = await _repository.GetAllAsync();
        var dtos = categorias.Select(c => new CategoriaDTO
        {
            Id = c.Id,
            Nombre = c.Nombre,
            FechaCreacion = c.FechaCreacion
        });

        return Result<IEnumerable<CategoriaDTO>>.Success(dtos);
    }
}
```

**Archivo**: `ApiPeliculas.Application/Features/Categorias/Queries/GetCategoriaByIdQuery.cs`

```csharp
using ApiPeliculas.Application.Common;
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Domain.Interfaces;
using MediatR;

namespace ApiPeliculas.Application.Features.Categorias.Queries;

public record GetCategoriaByIdQuery(int Id) : IRequest<Result<CategoriaDTO>>;

public class GetCategoriaByIdQueryHandler : IRequestHandler<GetCategoriaByIdQuery, Result<CategoriaDTO>>
{
    private readonly ICategoriaRepository _repository;

    public GetCategoriaByIdQueryHandler(ICategoriaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CategoriaDTO>> Handle(GetCategoriaByIdQuery request, CancellationToken cancellationToken)
    {
        var categoria = await _repository.GetByIdAsync(request.Id);
        if (categoria == null)
            return Result<CategoriaDTO>.Failure($"No se encontró la categoría con ID {request.Id}");

        return Result<CategoriaDTO>.Success(new CategoriaDTO
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            FechaCreacion = categoria.FechaCreacion
        });
    }
}
```

### 4.5 Películas (similar estructura)

Patrón idéntico para películas:
- `GetPeliculasQuery` (con paginación)
- `GetPeliculaByIdQuery`
- `GetPeliculasByCategoriaQuery`
- `SearchPeliculasQuery`
- `CrearPeliculaCommand` (con lógica de archivos)
- `ActualizarPeliculaCommand`
- `EliminarPeliculaCommand`

### 4.6 Usuarios

**Archivo**: `ApiPeliculas.Application/Features/Usuarios/Commands/RegistrarUsuarioCommand.cs`

```csharp
using ApiPeliculas.Application.Common;
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Domain.Interfaces;
using MediatR;

namespace ApiPeliculas.Application.Features.Usuarios.Commands;

public record RegistrarUsuarioCommand(
    string NombreUsuario, 
    string Nombre, 
    string Password, 
    string Role
) : IRequest<Result<UsuarioLoginRespuestaDTO>>;

public class RegistrarUsuarioCommandHandler : IRequestHandler<RegistrarUsuarioCommand, Result<UsuarioLoginRespuestaDTO>>
{
    private readonly IUsuarioRepository _repository;

    public RegistrarUsuarioCommandHandler(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UsuarioLoginRespuestaDTO>> Handle(RegistrarUsuarioCommand request, CancellationToken cancellationToken)
    {
        if (!await _repository.IsUsernameUniqueAsync(request.NombreUsuario))
            return Result<UsuarioLoginRespuestaDTO>.Failure("El nombre de usuario ya existe");

        var (usuario, token, role) = await _repository.RegisterAsync(
            request.NombreUsuario, 
            request.Nombre, 
            request.Password, 
            request.Role
        );

        if (usuario == null)
            return Result<UsuarioLoginRespuestaDTO>.Failure("Error en el registro");

        return Result<UsuarioLoginRespuestaDTO>.Success(new UsuarioLoginRespuestaDTO
        {
            Usuario = new UsuarioDTO
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                Nombre = usuario.Nombre,
                Role = usuario.Role
            },
            Role = role!,
            Token = token!
        });
    }
}
```

**Archivo**: `ApiPeliculas.Application/Features/Usuarios/Queries/LoginUsuarioQuery.cs`

```csharp
using ApiPeliculas.Application.Common;
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Domain.Interfaces;
using MediatR;

namespace ApiPeliculas.Application.Features.Usuarios.Queries;

public record LoginUsuarioQuery(string NombreUsuario, string Password) : IRequest<Result<UsuarioLoginRespuestaDTO>>;

public class LoginUsuarioQueryHandler : IRequestHandler<LoginUsuarioQuery, Result<UsuarioLoginRespuestaDTO>>
{
    private readonly IUsuarioRepository _repository;

    public LoginUsuarioQueryHandler(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UsuarioLoginRespuestaDTO>> Handle(LoginUsuarioQuery request, CancellationToken cancellationToken)
    {
        var (usuario, token, role) = await _repository.LoginAsync(request.NombreUsuario, request.Password);

        if (usuario == null || string.IsNullOrEmpty(token))
            return Result<UsuarioLoginRespuestaDTO>.Failure("El nombre de usuario o password son incorrectos");

        return Result<UsuarioLoginRespuestaDTO>.Success(new UsuarioLoginRespuestaDTO
        {
            Usuario = new UsuarioDTO
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                Nombre = usuario.Nombre,
                Role = usuario.Role
            },
            Role = role!,
            Token = token
        });
    }
}
```

### 4.7 Behaviors (Cross-cutting concerns)

**Archivo**: `ApiPeliculas.Application/Behaviors/ValidationBehavior.cs`

```csharp
using FluentValidation;
using MediatR;

namespace ApiPeliculas.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new FluentValidation.ValidationException(failures);

        return await next();
    }
}
```

### 4.8 Validaciones con FluentValidation

**Archivo**: `ApiPeliculas.Application/Validations/CrearCategoriaValidator.cs`

```csharp
using ApiPeliculas.Application.Features.Categorias.Commands;
using FluentValidation;

namespace ApiPeliculas.Application.Validations;

public class CrearCategoriaValidator : AbstractValidator<CrearCategoriaCommand>
{
    public CrearCategoriaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");
    }
}
```

### 4.9 Resultado de esta fase

```
ApiPeliculas.Application/
├── ApiPeliculas.Application.csproj
├── Common/
│   └── Result.cs
├── DTOs/
│   ├── CategoriaDTO.cs
│   ├── PeliculaDTO.cs
│   └── UsuarioDTO.cs
├── Features/
│   ├── Categorias/
│   │   ├── Commands/
│   │   │   ├── CrearCategoriaCommand.cs
│   │   │   ├── ActualizarCategoriaCommand.cs
│   │   │   └── EliminarCategoriaCommand.cs
│   │   └── Queries/
│   │       ├── GetCategoriasQuery.cs
│   │       └── GetCategoriaByIdQuery.cs
│   ├── Peliculas/
│   │   └── ...
│   └── Usuarios/
│       └── ...
├── Behaviors/
│   └── ValidationBehavior.cs
└── Validations/
    └── CrearCategoriaValidator.cs
```

---

## 5. Fase 3: Capa de Infraestructura (Infrastructure Layer) (Día 4-6)

### Objetivo
Implementar todas las dependencias externas: EF Core, Identity, FileStorage, JWT.

### 5.1 Crear proyecto Infrastructure

```bash
dotnet new classlib -n ApiPeliculas.Infrastructure -o ApiPeliculas.Infrastructure
dotnet sln add ApiPeliculas.Infrastructure/ApiPeliculas.Infrastructure.csproj
```

**Agregar referencias**:
```bash
dotnet add ApiPeliculas.Infrastructure/ApiPeliculas.Infrastructure.csproj reference ApiPeliculas.Domain/ApiPeliculas.Domain.csproj
dotnet add ApiPeliculas.Infrastructure/ApiPeliculas.Infrastructure.csproj reference ApiPeliculas.Application/ApiPeliculas.Application.csproj
```

**Agregar paquetes**:
```bash
dotnet add ApiPeliculas.Infrastructure/ApiPeliculas.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.4
dotnet add ApiPeliculas.Infrastructure/ApiPeliculas.Infrastructure.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.0
dotnet add ApiPeliculas.Infrastructure/ApiPeliculas.Infrastructure.csproj package System.IdentityModel.Tokens.Jwt --version 8.0.0
dotnet add ApiPeliculas.Infrastructure/ApiPeliculas.Infrastructure.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
```

### 5.2 Migrar DbContext

**Archivo**: `ApiPeliculas.Infrastructure/Persistence/ApplicationDbContext.cs`

```csharp
using ApiPeliculas.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApiPeliculas.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Pelicula> Peliculas { get; set; }
    public DbSet<DomainUser> DomainUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        builder.Entity<Pelicula>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Categoria)
                  .WithMany(c => c.Peliculas)
                  .HasForeignKey(e => e.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

// Entidad de Identity para Infrastructure (separa del Domain)
public class AppUser : IdentityUser
{
    public string Nombre { get; set; } = string.Empty;
}

// Entidad para mapeo de Identity a Domain
public class DomainUser
{
    public string Id { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

### 5.3 Implementar Repositorios

**Archivo**: `ApiPeliculas.Infrastructure/Persistence/Repositories/CategoriaRepository.cs`

```csharp
using ApiPeliculas.Domain.Entities;
using ApiPeliculas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiPeliculas.Infrastructure.Persistence.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly ApplicationDbContext _context;

    public CategoriaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Categoria>> GetAllAsync()
    {
        return await _context.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Categoria?> GetByIdAsync(int id)
    {
        return await _context.Categorias.FindAsync(id);
    }

    public async Task<bool> ExistsByNameAsync(string nombre)
    {
        return await _context.Categorias
            .AnyAsync(c => c.Nombre.ToLower().Trim() == nombre.ToLower().Trim());
    }

    public async Task<bool> ExistsByIdAsync(int id)
    {
        return await _context.Categorias.AnyAsync(c => c.Id == id);
    }

    public async Task AddAsync(Categoria categoria)
    {
        await _context.Categorias.AddAsync(categoria);
    }

    public Task UpdateAsync(Categoria categoria)
    {
        _context.Categorias.Update(categoria);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Categoria categoria)
    {
        _context.Categorias.Remove(categoria);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

**Archivo**: `ApiPeliculas.Infrastructure/Persistence/Repositories/PeliculaRepository.cs`

```csharp
using ApiPeliculas.Domain.Entities;
using ApiPeliculas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiPeliculas.Infrastructure.Persistence.Repositories;

public class PeliculaRepository : IPeliculaRepository
{
    private readonly ApplicationDbContext _context;

    public PeliculaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Pelicula> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.Peliculas.CountAsync();
        var items = await _context.Peliculas
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Pelicula?> GetByIdAsync(int id)
    {
        return await _context.Peliculas
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Pelicula>> GetByCategoriaAsync(int categoriaId)
    {
        return await _context.Peliculas
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.CategoriaId == categoriaId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Pelicula>> SearchAsync(string nombre)
    {
        return await _context.Peliculas
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.Nombre.Contains(nombre))
            .ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(string nombre)
    {
        return await _context.Peliculas.AnyAsync(p => p.Nombre == nombre);
    }

    public async Task<bool> ExistsByIdAsync(int id)
    {
        return await _context.Peliculas.AnyAsync(p => p.Id == id);
    }

    public async Task AddAsync(Pelicula pelicula)
    {
        await _context.Peliculas.AddAsync(pelicula);
    }

    public Task UpdateAsync(Pelicula pelicula)
    {
        _context.Peliculas.Update(pelicula);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Pelicula pelicula)
    {
        _context.Peliculas.Remove(pelicula);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

### 5.4 Implementar servicios de infraestructura

**Archivo**: `ApiPeliculas.Infrastructure/Services/FileStorageService.cs`

```csharp
using ApiPeliculas.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace ApiPeliculas.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
    private readonly string[] _allowedTypes = { "image/jpeg", "image/png", "image/gif" };

    public FileStorageService(IWebHostEnvironment environment, string baseUrl)
    {
        _environment = environment;
        _basePath = Path.Combine(environment.WebRootPath, "ImagenesPeliculas");
        _baseUrl = baseUrl;
        
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        if (!IsValidFileType(contentType))
            throw new InvalidOperationException("Tipo de archivo no válido");
        
        if (fileStream.Length > _maxFileSize)
            throw new InvalidOperationException("Archivo excede el tamaño máximo permitido");

        var extension = Path.GetExtension(fileName);
        var uniqueName = Guid.NewGuid().ToString() + extension;
        var filePath = Path.Combine(_basePath, uniqueName);

        using (var file = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(file);
        }

        return uniqueName;
    }

    public Task DeleteFileAsync(string fileName)
    {
        var filePath = Path.Combine(_basePath, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public string GetFileUrl(string fileName)
    {
        return $"{_baseUrl}/ImagenesPeliculas/{fileName}";
    }

    public bool IsValidFileType(string contentType)
    {
        return _allowedTypes.Contains(contentType.ToLower());
    }

    public bool IsValidFileSize(long fileSize)
    {
        return fileSize <= _maxFileSize;
    }
}
```

**Archivo**: `ApiPeliculas.Infrastructure/Services/JwtService.cs`

```csharp
using ApiPeliculas.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiPeliculas.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtService(IConfiguration configuration)
    {
        _secret = configuration["JwtSettings:Secret"]!;
        _issuer = configuration["JwtSettings:Issuer"]!;
        _audience = configuration["JwtSettings:Audience"]!;
        _expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryMinutes"]!);
    }

    public string GenerateToken(string userId, string username, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(_expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### 5.5 Implementar UsuarioRepository con Identity

**Archivo**: `ApiPeliculas.Infrastructure/Identity/UsuarioRepository.cs`

```csharp
using ApiPeliculas.Domain.Entities;
using ApiPeliculas.Domain.Interfaces;
using ApiPeliculas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApiPeliculas.Infrastructure.Identity;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ApplicationDbContext _context;

    public UsuarioRepository(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJwtService jwtService,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _context = context;
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        return users.Select(u => new Usuario(u.Id, u.UserName!, u.Nombre, 
            _userManager.GetRolesAsync(u).Result.FirstOrDefault() ?? "User"));
    }

    public async Task<Usuario?> GetByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new Usuario(user.Id, user.UserName!, user.Nombre, roles.FirstOrDefault() ?? "User");
    }

    public async Task<bool> IsUsernameUniqueAsync(string username)
    {
        return await _userManager.FindByNameAsync(username) == null;
    }

    public async Task<(Usuario? Usuario, string? Token, string? Role)> RegisterAsync(
        string username, string nombre, string password, string role)
    {
        var user = new AppUser
        {
            UserName = username,
            Email = username,
            Nombre = nombre
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (null, null, null);

        await _userManager.AddToRoleAsync(user, role);
        var token = _jwtService.GenerateToken(user.Id, user.UserName!, role);

        return (new Usuario(user.Id, user.UserName!, user.Nombre, role), token, role);
    }

    public async Task<(Usuario? Usuario, string? Token, string? Role)> LoginAsync(
        string username, string password)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return (null, null, null);

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded)
            return (null, null, null);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";
        var token = _jwtService.GenerateToken(user.Id, user.UserName!, role);

        return (new Usuario(user.Id, user.UserName!, user.Nombre, role), token, role);
    }
}
```

### 5.6 Migrations

Las migrations existentes se mantienen. No es necesario recrearlas. Solo asegurar que la estructura de tablas de la nueva capa de infraestructura sea compatible.

**Comando de verificación**:
```bash
dotnet ef migrations add InitialCleanArchitecture --project ApiPeliculas.Infrastructure --startup-project ApiPeliculas.API
```

> **Nota**: Como la base de datos ya existe, el primer migration será vacío o snapshot. No debe modificar la base de datos.

### 5.7 Resultado de esta fase

```
ApiPeliculas.Infrastructure/
├── ApiPeliculas.Infrastructure.csproj
├── Persistence/
│   ├── ApplicationDbContext.cs
│   └── Repositories/
│       ├── CategoriaRepository.cs
│       ├── PeliculaRepository.cs
│       └── UsuarioRepository.cs
├── Identity/
│   └── UsuarioRepository.cs
└── Services/
    ├── FileStorageService.cs
    └── JwtService.cs
```

---

## 6. Fase 4: Capa de API (Presentation Layer) (Día 6-8)

### Objetivo
Refactorizar controllers para que sean delgados y deleguen a la capa de aplicación.

### 6.1 Crear proyecto API

```bash
dotnet new webapi -n ApiPeliculas.API -o ApiPeliculas.API
dotnet sln add ApiPeliculas.API/ApiPeliculas.API.csproj
```

**Agregar referencias**:
```bash
dotnet add ApiPeliculas.API/ApiPeliculas.API.csproj reference ApiPeliculas.Application/ApiPeliculas.Application.csproj
dotnet add ApiPeliculas.API/ApiPeliculas.API.csproj reference ApiPeliculas.Infrastructure/ApiPeliculas.Infrastructure.csproj
```

**Agregar paquetes** (mantener los existentes):
```bash
dotnet add ApiPeliculas.API/ApiPeliculas.API.csproj package Asp.Versioning.Mvc --version 8.0.0
dotnet add ApiPeliculas.API/ApiPeliculas.API.csproj package Asp.Versioning.Mvc.ApiExplorer --version 8.0.0
dotnet add ApiPeliculas.API/ApiPeliculas.API.csproj package AutoMapper --version 13.0.1
dotnet add ApiPeliculas.API/ApiPeliculas.API.csproj package Swashbuckle.AspNetCore --version 6.6.2
dotnet add ApiPeliculas.API/ApiPeliculas.API.csproj package FluentValidation --version 11.9.0
dotnet add ApiPeliculas.API/ApiPeliculas.API.csproj package FluentValidation.DependencyInjectionExtensions --version 11.9.0
```

### 6.2 Configurar Program.cs

**Archivo**: `ApiPeliculas.API/Program.cs`

```csharp
using ApiPeliculas.Application.Behaviors;
using ApiPeliculas.Domain.Interfaces;
using ApiPeliculas.Infrastructure.Identity;
using ApiPeliculas.Infrastructure.Persistence;
using ApiPeliculas.Infrastructure.Persistence.Repositories;
using ApiPeliculas.Infrastructure.Services;
using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSql")));

// 2. Identity
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. Dependency Injection - Repositories
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IPeliculaRepository, PeliculaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// 4. Dependency Injection - Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFileStorageService>(provider =>
{
    var env = provider.GetRequiredService<IWebHostEnvironment>();
    var httpContext = provider.GetRequiredService<IHttpContextAccessor>();
    var baseUrl = $"{httpContext.HttpContext?.Request.Scheme}://{httpContext.HttpContext?.Request.Host.Value}";
    return new FileStorageService(env, baseUrl);
});

// 5. MediatR + CQRS
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.RegisterServicesFromAssembly(typeof(ApiPeliculas.Application.Features.Categorias.Commands.CrearCategoriaCommand).Assembly);
});

// 6. FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(ApiPeliculas.Application.Features.Categorias.Commands.CrearCategoriaCommand).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// 7. AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// 8. JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 9. API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// 10. Response Caching
builder.Services.AddResponseCaching();
builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("PorDefecto30Segundos", new CacheProfile { Duration = 30 });
});

// 11. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaCors", builder =>
    {
        builder.WithOrigins("http://localhost:5103")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// 12. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT usando el esquema Bearer. \\r\\n\\r\\n Ingresa la palabra 'Bearer' seguido de un [espacio] y después su token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.SwaggerDoc("v1", new OpenApiInfo { Version = "v1.0", Title = "Peliculas Api V1" });
    options.SwaggerDoc("v2", new OpenApiInfo { Version = "v2.0", Title = "Peliculas Api V2" });
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Middleware Pipeline
app.UseSwagger();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiPeliculasV1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "ApiPeliculasV2");
    });
}
else
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiPeliculasV1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "ApiPeliculasV2");
        options.RoutePrefix = "";
    });
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("PoliticaCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCaching();
app.MapControllers();

app.Run();
```

### 6.3 Refactorizar Controllers

#### V1 - CategoriasController

**Archivo**: `ApiPeliculas.API/Controllers/V1/CategoriasController.cs`

```csharp
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Application.Features.Categorias.Commands;
using ApiPeliculas.Application.Features.Categorias.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPeliculas.API.Controllers.V1;

[Route("api/v{version:apiVersion}/categorias")]
[ApiController]
[ApiVersion("1.0")]
public class CategoriasController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet]
    [ResponseCache(CacheProfileName = "PorDefecto30Segundos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategorias()
    {
        var result = await _mediator.Send(new GetCategoriasQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}", Name = "GetCategoria")]
    [ResponseCache(CacheProfileName = "PorDefecto30Segundos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoria(int id)
    {
        var result = await _mediator.Send(new GetCategoriaByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearCategoria([FromBody] CrearCategoriaDTO dto)
    {
        var result = await _mediator.Send(new CrearCategoriaCommand(dto.Nombre));
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtRoute("GetCategoria", new { id = result.Value!.Id }, result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActualizarCategoria(int id, [FromBody] ActualizarCategoriaDTO dto)
    {
        if (id != dto.Id)
            return BadRequest("El ID no coincide");

        var result = await _mediator.Send(new ActualizarCategoriaCommand(id, dto.Nombre));
        
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarCategoria(int id)
    {
        var result = await _mediator.Send(new EliminarCategoriaCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}
```

#### V1 - PeliculasController

```csharp
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Application.Features.Peliculas.Commands;
using ApiPeliculas.Application.Features.Peliculas.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPeliculas.API.Controllers.V1;

[Route("api/v{version:apiVersion}/peliculas")]
[ApiController]
[ApiVersion("1.0")]
public class PeliculasController : ControllerBase
{
    private readonly IMediator _mediator;

    public PeliculasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPeliculas([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 2)
    {
        var result = await _mediator.Send(new GetPeliculasQuery(pageNumber, pageSize));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}", Name = "GetPelicula")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPelicula(int id)
    {
        var result = await _mediator.Send(new GetPeliculaByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearPelicula([FromForm] CrearPeliculaDTO dto)
    {
        var result = await _mediator.Send(new CrearPeliculaCommand(dto));
        return result.IsSuccess 
            ? CreatedAtRoute("GetPelicula", new { id = result.Value!.Id }, result.Value)
            : BadRequest(result.Error);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ActualizarPelicula(int id, [FromForm] ActualizarPeliculaDTO dto)
    {
        if (id != dto.Id)
            return BadRequest("El ID no coincide");

        var result = await _mediator.Send(new ActualizarPeliculaCommand(dto));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarPelicula(int id)
    {
        var result = await _mediator.Send(new EliminarPeliculaCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [AllowAnonymous]
    [HttpGet("GetPeliculasEnCategoria/{categoriaId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPeliculasEnCategoria(int categoriaId)
    {
        var result = await _mediator.Send(new GetPeliculasByCategoriaQuery(categoriaId));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [AllowAnonymous]
    [HttpGet("Buscar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Buscar(string nombre)
    {
        var result = await _mediator.Send(new SearchPeliculasQuery(nombre));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
```

#### UsuariosController

```csharp
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Application.Features.Usuarios.Commands;
using ApiPeliculas.Application.Features.Usuarios.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPeliculas.API.Controllers;

[Route("api/v{version:apiVersion}/usuarios")]
[ApiController]
[ApiVersionNeutral]
public class UsuariosController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsuariosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsuarios()
    {
        var result = await _mediator.Send(new GetUsuariosQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsuario(string id)
    {
        var result = await _mediator.Send(new GetUsuarioByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [AllowAnonymous]
    [HttpPost("registro")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registro([FromBody] UsuarioRegistroDTO dto)
    {
        var result = await _mediator.Send(new RegistrarUsuarioCommand(
            dto.NombreUsuario, dto.Nombre, dto.Password, dto.Role));
        
        return result.IsSuccess ? Created("", result.Value) : BadRequest(result.Error);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] UsuarioLoginDTO dto)
    {
        var result = await _mediator.Send(new LoginUsuarioQuery(dto.NombreUsuario, dto.Password));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
```

### 6.4 Middleware Global de Excepciones

**Archivo**: `ApiPeliculas.API/Middleware/ExceptionMiddleware.cs`

```csharp
using ApiPeliculas.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace ApiPeliculas.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await HandleExceptionAsync(context, HttpStatusCode.BadRequest, ex.Errors);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, HttpStatusCode.NotFound, new List<string> { ex.Message });
        }
        catch (DomainException ex)
        {
            await HandleExceptionAsync(context, HttpStatusCode.BadRequest, new List<string> { ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado");
            await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, 
                new List<string> { "Error interno del servidor" });
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, List<string> errors)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            StatusCode = (int)statusCode,
            IsSuccess = false,
            ErrorMessages = errors,
            Result = (object?)null
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

**Registrar en Program.cs**:
```csharp
app.UseMiddleware<ExceptionMiddleware>();
```

### 6.5 AutoMapper Profile

**Archivo**: `ApiPeliculas.API/Mappings/MappingProfile.cs`

```csharp
using ApiPeliculas.Application.DTOs;
using ApiPeliculas.Domain.Entities;
using AutoMapper;

namespace ApiPeliculas.API.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Categoria
        CreateMap<Categoria, CategoriaDTO>();
        
        // Pelicula
        CreateMap<Pelicula, PeliculaDTO>()
            .ForMember(dest => dest.Clasificacion, opt => opt.MapFrom(src => src.Clasificacion.ToString()));
        
        // Usuario
        CreateMap<Usuario, UsuarioDTO>();
    }
}
```

### 6.6 Resultado de esta fase

```
ApiPeliculas.API/
├── ApiPeliculas.API.csproj
├── Controllers/
│   ├── V1/
│   │   ├── CategoriasController.cs
│   │   └── PeliculasController.cs
│   ├── V2/
│   │   └── CategoriasController.cs
│   └── UsuariosController.cs
├── Middleware/
│   └── ExceptionMiddleware.cs
├── Mappings/
│   └── MappingProfile.cs
├── wwwroot/
│   └── ImagenesPeliculas/
├── Program.cs
└── appsettings.json
```

---

## 7. Fase 5: Configuración y Seguridad (Día 8-9)

### 7.1 Actualizar appsettings.json

```json
{
  "JwtSettings": {
    "Secret": "Curso de Api Restfull render2web - Clave super segura para JWT",
    "Issuer": "ApiPeliculas",
    "Audience": "ApiPeliculasClient",
    "ExpiryMinutes": 60
  },
  "ConnectionStrings": {
    "ConexionSql": "Data Source=localhost;TrustServerCertificate=True;MultiSubnetFailover=True;Initial Catalog=ApiPeliculasNET8;user id=sa;password=r34llyStr0ngPwd123"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 7.2 Seguridad mejorada

| Aspecto | Antes | Después |
|---------|-------|---------|
| **JWT Validation** | `ValidateIssuer=false`, `ValidateAudience=false` | `ValidateIssuer=true`, `ValidateAudience=true` |
| **JWT Secret** | Corto, sin encoding UTF8 | Largo, Encoding.UTF8, configuración estructurada |
| **HTTPS** | `RequireHttpsMetadata=false` | `false` en dev, `true` en prod (por environment) |
| **File Upload** | Sin validación | Validación de tipo MIME, tamaño máximo, sanitización |
| **Passwords** | Identity default | Identity + configuración de complejidad |
| **Rate Limiting** | Sin implementar | Middleware `AspNetCoreRateLimit` recomendado |
| **Headers** | Sin configurar | `X-Content-Type-Options`, `X-Frame-Options`, `HSTS` |

### 7.3 Agregar Rate Limiting (opcional pero recomendado)

```bash
dotnet add package AspNetCoreRateLimit --version 5.0.0
```

**Configuración en Program.cs**:
```csharp
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 100,
            Period = "1m"
        }
    };
});
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
```

---

## 8. Fase 6: Migración de Base de Datos (Día 9)

### 8.1 Estrategia

Como la base de datos ya existe y tiene datos:
1. **No eliminar** la base de datos existente.
2. **Crear un snapshot** del modelo actual en Infrastructure.
3. **Verificar compatibilidad** entre entidades de Domain y tablas existentes.
4. **Si hay cambios**, crear migration vacía + migration con cambios necesarios.

### 8.2 Comandos

```bash
# Desde la raíz del solution
# 1. Asegurar que Infrastructure tiene el DbContext
# 2. Crear migration inicial (debería ser vacía o con mínimos ajustes)
dotnet ef migrations add InitialCleanArchitecture \
  --project ApiPeliculas.Infrastructure \
  --startup-project ApiPeliculas.API \
  --output-dir Persistence/Migrations

# 3. Verificar SQL generado (sin aplicar)
dotnet ef migrations script \
  --project ApiPeliculas.Infrastructure \
  --startup-project ApiPeliculas.API

# 4. Si todo es compatible, aplicar
dotnet ef database update \
  --project ApiPeliculas.Infrastructure \
  --startup-project ApiPeliculas.API
```

### 8.3 Compatibilidad a verificar

- Nombres de tablas: `Categorias` vs `Categoria`
- Nombres de columnas: `CategoriaId` vs `categoriaId`
- Tipos de datos: `enum` en C# vs `int` en SQL
- Constraints: `UNIQUE` en nombre de categoría
- Indices: Revisar índices existentes

---

## 9. Fase 7: Testing y Validación (Día 10)

### 9.1 Tests de integración

```bash
dotnet new xunit -n ApiPeliculas.Tests -o ApiPeliculas.Tests
dotnet sln add ApiPeliculas.Tests/ApiPeliculas.Tests.csproj
dotnet add ApiPeliculas.Tests/ApiPeliculas.Tests.csproj reference ApiPeliculas.API/ApiPeliculas.API.csproj
```

**Ejemplo de test de integración**:
```csharp
using ApiPeliculas.API;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ApiPeliculas.Tests.Integration;

public class CategoriasControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CategoriasControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategorias_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1.0/categorias");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

### 9.2 Checklist de validación

- [ ] Compilar solution completa: `dotnet build`
- [ ] Correr API: `dotnet run --project ApiPeliculas.API`
- [ ] Swagger accesible: `http://localhost:5103/swagger`
- [ ] Endpoints v1 respondiendo igual que antes
- [ ] Endpoints v2 respondiendo igual que antes
- [ ] Autenticación JWT funcionando
- [ ] Autorización por roles funcionando
- [ ] Subida de imágenes funcionando
- [ ] Paginación de películas funcionando
- [ ] Búsqueda de películas funcionando
- [ ] Cache de 30 segundos funcionando
- [ ] CORS funcionando
- [ ] Base de datos intacta (sin pérdida de datos)
- [ ] Migrations aplicables sin errores

---

## 10. Fase 8: Limpieza y Deprecación (Día 10-11)

### 10.1 Eliminar proyecto legacy

Una vez validado que todo funciona:

```bash
# 1. Remover proyecto antiguo de la solución
dotnet sln remove ApiPeliculas/ApiPeliculas.csproj

# 2. Mover archivos estáticos (si no se movieron ya)
# wwwroot/ImagenesPeliculas → ApiPeliculas.API/wwwroot/ImagenesPeliculas

# 3. Eliminar carpeta antigua (con precaución)
# rm -rf ApiPeliculas/
```

### 10.2 Actualizar AGENTS.md

Documentar la nueva arquitectura:
- `dotnet run --project ApiPeliculas.API`
- Migrations: `dotnet ef database update --project ApiPeliculas.Infrastructure --startup-project ApiPeliculas.API`

---

## 11. Timeline y Estimación

| Fase | Días | Esfuerzo | Complejidad |
|------|------|----------|---------------|
| 0. Preparación | 0.5 | Bajo | Baja |
| 1. Domain Layer | 1-2 | Medio | Media |
| 2. Application Layer | 2-3 | Alto | Alta |
| 3. Infrastructure Layer | 2-3 | Alto | Alta |
| 4. API Layer | 2-3 | Alto | Media |
| 5. Configuración | 1-2 | Medio | Media |
| 6. Database Migration | 0.5 | Bajo | Media |
| 7. Testing | 1 | Medio | Media |
| 8. Cleanup | 0.5 | Bajo | Baja |
| **Total** | **10-12 días** | | |

---

## 12. Rollback Plan

En cada fase, si algo falla:

1. **Fase 0-3**: Simplemente no agregar referencias al nuevo proyecto. El proyecto legacy sigue funcionando.
2. **Fase 4**: Si el nuevo API falla, el legacy sigue disponible. No eliminar hasta validar.
3. **Fase 6**: Si la migration falla:
   ```bash
   dotnet ef database update <migration-anterior> --project ApiPeliculas.Infrastructure --startup-project ApiPeliculas.API
   ```
4. **Git**: Cada fase es un commit. `git revert` o `git checkout` al commit anterior.

---

## 13. Diagrama de Arquitectura Final

```
┌─────────────────────────────────────────────────────────────┐
│                    ApiPeliculas.API                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ Controllers │  │ Middleware  │  │  Program.cs │          │
│  │   (V1/V2)   │  │ (Exception) │  │   (DI)      │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│              │                                              │
│              │ HTTP / API Contracts                          │
│              ▼                                              │
├─────────────────────────────────────────────────────────────┤
│                ApiPeliculas.Application                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │   MediatR   │  │   DTOs      │  │Behaviors    │          │
│  │  Commands   │  │ Validations │  │(Validation) │          │
│  │  Queries    │  │  Mappings   │  │             │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│              │                                              │
│              │ Interfaces (Contracts)                        │
│              ▼                                              │
├─────────────────────────────────────────────────────────────┤
│              ApiPeliculas.Infrastructure                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │   EF Core   │  │   Identity  │  │  Services   │          │
│  │ Repositories│  │  (JWT)      │  │(FileStorage)│          │
│  │  DbContext  │  │             │  │             │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│              │                                              │
│              │ Implementaciones concretas                    │
│              ▼                                              │
├─────────────────────────────────────────────────────────────┤
│                  ApiPeliculas.Domain                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │  Entities   │  │  Interfaces │  │  Exceptions │          │
│  │ (Categoria, │  │ (Repository,│  │  (Domain)   │          │
│  │  Pelicula)  │  │  Services)  │  │             │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│                                                             │
│              Sin dependencias de frameworks                  │
└─────────────────────────────────────────────────────────────┘
              │
              │ SQL / TCP / FileSystem
              ▼
        ┌─────────────┐
        │  SQL Server │
        │   (Local)   │
        └─────────────┘
```

---

## 14. Dependencias de Proyectos

```
ApiPeliculas.Domain
    │
    ▼ (referencia)
ApiPeliculas.Application
    │
    ▼ (referencia)
ApiPeliculas.Infrastructure
    │
    ▼ (referencia)
ApiPeliculas.API
```

**Regla de oro**: Las dependencias apuntan siempre hacia adentro. `Domain` no depende de nadie. `API` depende de todos.

---

## 15. Conclusión

Este plan de migración es **incremental, seguro y reversible**. Cada fase produce un artefacto funcional y no requiere "big bang".

**Beneficios tras la migración**:
- ✅ Testabilidad: Unit tests para Domain, Integration tests para Application
- ✅ Mantenibilidad: Cambios en DB no afectan controllers
- ✅ Escalabilidad: Nuevas features se agregan como nuevos casos de uso
- ✅ Seguridad: JWT con validación completa, rate limiting, validación de archivos
- ✅ Performance: `AsNoTracking`, `ProjectTo`, `CancellationToken`
- ✅ Flexibilidad: Cambiar EF Core por Dapper, o SQL Server por PostgreSQL, sin tocar controllers

**Riesgo**: 10-12 días de esfuerzo. Mitigación: Fase por fase, rollback disponible en cada paso.

---

*Documento generado como guía de migración arquitectónica.*
*Próxima acción: Iniciar Fase 0 (Preparación).*