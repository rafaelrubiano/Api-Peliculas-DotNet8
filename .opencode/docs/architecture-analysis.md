# Análisis Arquitectónico - ApiPeliculas

> **Fecha de análisis**: 2026-06-10
> **Proyecto**: ApiPeliculas (.NET 8 RESTful API)
> **Estado actual**: Arquitectura monolítica tradicional (N-Layer básica)
> **Objetivo**: Documentar estado actual, identificar deuda técnica, y definir brecha hacia Clean Architecture

---

## 1. Resumen Ejecutivo

Esta API **NO sigue Clean Architecture**. Es una aplicación monolítica de un solo proyecto que implementa un patrón N-Layer básico con Repository Pattern, pero carece de la separación fundamental de capas, la independencia de frameworks, y la encapsulación de lógica de negocio que define Clean Architecture.

**Puntuación de madurez arquitectónica**:
- Clean Architecture: **2/10** (tiene Repository Pattern y DI, pero nada más)
- SOLID: **4/10** (DIP violado, SRP violado en controllers)
- Mantenibilidad: **5/10** (código organizado pero altamente acoplado)
- Testabilidad: **4/10** (sin capa de aplicación, tests difíciles de escribir)

---

## 2. Estado Actual de la Arquitectura

### 2.1 Estructura del Proyecto

```
ApiPeliculas/ (single project)
├── Controllers/
│   ├── V1/
│   │   ├── CategoriasController.cs
│   │   └── PeliculasController.cs
│   ├── V2/
│   │   └── CategoriasController.cs
│   └── UsuariosController.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Modelos/
│   ├── Categoria.cs
│   ├── Pelicula.cs
│   ├── Usuario.cs
│   ├── AppUsuario.cs
│   ├── RespuestaAPI.cs
│   └── Dtos/
│       ├── CategoriaDto.cs
│       ├── CrearCategoriaDto.cs
│       ├── PeliculaDto.cs
│       ├── CrearPeliculaDto.cs
│       ├── ActualizarPeliculaDto.cs
│       ├── UsuarioDto.cs
│       ├── UsuarioDatosDto.cs
│       ├── UsuarioLoginDto.cs
│       ├── UsuarioLoginRespuestaDto.cs
│       └── UsuarioRegistroDto.cs
├── Repositorio/
│   ├── IRepositorio/
│   │   ├── ICategoriaRepositorio.cs
│   │   ├── IPeliculaRepositorio.cs
│   │   └── IUsuarioRepositorio.cs
│   ├── CategoriaRepositorio.cs
│   ├── PeliculaRepositorio.cs
│   └── UsuarioRepositorio.cs
├── PeliculasMappers/
│   └── PeliculasMapper.cs
├── Migrations/
├── wwwroot/
│   └── ImagenesPeliculas/
├── Program.cs
└── ApiPeliculas.csproj
```

### 2.2 Decisiones Arquitectónicas Actuales

| Aspecto | Implementación Actual | Nota |
|---------|----------------------|------|
| **Proyectos** | Single project (`ApiPeliculas.csproj`) | Todo en un solo assembly |
| **Capas** | Implícitas (carpetas) | No hay separación física ni semántica |
| **Base de datos** | EF Core 8.0.4 + SQL Server | DbContext conocido por toda la app |
| **Autenticación** | JWT Bearer + ASP.NET Core Identity | Configurado en `Program.cs` |
| **Mapeo** | AutoMapper 13.0.1 | Perfil centralizado en `PeliculasMapper` |
| **Versionamiento** | Asp.Versioning.Mvc 8.0.0 | V1 y V2 por URL |
| **Cache** | ResponseCaching global | 30 segundos por defecto |
| **CORS** | Restringido a localhost | `http://localhost:5103` |
| **Swagger** | Swashbuckle.AspNetCore 6.6.2 | Docs v1 y v2 |

---

## 3. Análisis por Capas (Clean Architecture vs Actual)

### 3.1 Capa de Dominio (Domain Layer) - ❌ AUSENTE

**Qué debería tener**:
- Entidades ricas con comportamiento encapsulado
- Value Objects inmutables
- Domain Events
- Excepciones de dominio personalizadas
- Interfaces de repositorio (contratos, no implementaciones)
- **Sin dependencias de frameworks**

**Qué tiene actualmente**:
- POCOs planos con Data Annotations (atributos de EF Core)
- Sin comportamiento de negocio
- Sin validaciones de dominio
- Sin Value Objects

**Ejemplo de entidad actual**:
```csharp
public class Categoria
{
    [Key] public int Id { get; set; }
    [Required] public string Nombre { get; set; }
    [Required] public DateTime FechaCreacion { get; set; }
}
```

**Problema**: Las entidades son anémicas. No hay encapsulamiento, no hay invariantes de negocio, y dependen de EF Core (`[Key]`, `[Required]`).

### 3.2 Capa de Aplicación (Application Layer) - ❌ AUSENTE

**Qué debería tener**:
- Casos de uso / Commands y Queries (CQRS)
- DTOs de aplicación (Input/Output)
- Validación de aplicación (FluentValidation)
- Servicios de aplicación (orquestación)
- Interfaces de servicios externos
- **Depende solo de Domain**

**Qué tiene actualmente**:
- No hay servicios de aplicación
- No hay CQRS / MediatR
- No hay FluentValidation
- Los controllers hablan directamente con repositorios

**Ejemplo de flujo actual**:
```
Controller → Repositorio → DbContext → SQL Server
```

**Problema**: El controller es el "orquestador", lo que viola SRP. No hay una capa que encapsule la lógica de aplicación, haciendo los controllers difíciles de testear y mantener.

### 3.3 Capa de Infraestructura (Infrastructure Layer) - ⚠️ MEZCLADA

**Qué debería tener**:
- Implementación de repositorios (EF Core)
- Servicios externos (email, file storage, HTTP clients)
- Configuración de base de datos
- Migrations
- **Depende de Application**

**Qué tiene actualmente**:
- Repositorios implementados en el mismo proyecto
- DbContext en `Data/`
- Identity configurado en `Program.cs`
- JWT configurado en `Program.cs`
- Manejo de archivos en controllers

**Problema**: La infraestructura está mezclada con la capa de presentación. No hay abstracción de servicios externos.

### 3.4 Capa de Presentación (API Layer) - ⚠️ SOBRECARGADA

**Qué debería tener**:
- Controllers delgados (solo reciben requests, devuelven responses)
- Mapeo DTO ↔ Command/Query
- Model binding y validation
- **Depende de Application**

**Qué tiene actualmente**:
- Controllers gruesos con lógica de negocio
- Validación de existencia en controllers
- Manejo de archivos (IFormFile) en controllers
- Generación de URLs en controllers
- Mapeo manual con loops

**Ejemplo de controller actual**:
```csharp
public IActionResult CrearCategoria([FromBody] CrearCategoriaDto dto)
{
    if (_ctRepo.ExisteCategoria(dto.Nombre))  // ← Validación de negocio
    {
        ModelState.AddModelError("", "La categoría ya existe!");
        return StatusCode(404, ModelState);
    }
    var categoria = _mapper.Map<Categoria>(dto);
    if (!_ctRepo.CrearCategoria(categoria))   // ← Lógica de persistencia
    {
        ModelState.AddModelError("", "Algo salió mal...");
        return StatusCode(500, ModelState);
    }
    return CreatedAtRoute("GetCategoria", new {CategoriaId = categoria.Id}, categoria);
}
```

---

## 4. Violaciones a Principios SOLID

### 4.1 Single Responsibility Principle (SRP) - 🔴 Violado

**Controllers** tienen múltiples responsabilidades:
- Recibir y validar HTTP requests
- Orquestar lógica de negocio
- Validar reglas de dominio (existencia, unicidad)
- Manejar persistencia
- Generar URLs de archivos
- Mapear entidades a DTOs

**Repositorios** tienen lógica de negocio:
```csharp
public bool CrearCategoria(Categoria categoria)
{
    categoria.FechaCreacion = DateTime.Now;  // ← Lógica de dominio
    _bd.Categoria.Add(categoria);
    return Guardar();
}
```

### 4.2 Open/Closed Principle (OCP) - 🟡 Parcial

- Repository Pattern permite extender repositorios sin modificar interfaces
- Pero los controllers no usan abstracciones de alto nivel (falta capa de aplicación)

### 4.3 Liskov Substitution Principle (LSP) - ✅ Cumplido

- Las interfaces de repositorio (`ICategoriaRepositorio`) permiten sustitución
- No hay herencia compleja que violaría LSP

### 4.4 Interface Segregation Principle (ISP) - ✅ Cumplido

- Cada entidad tiene su propia interfaz de repositorio
- No hay interfaces monolíticas

### 4.5 Dependency Inversion Principle (DIP) - 🔴 Violado

**Violación #1**: `Program.cs` depende directamente de EF Core:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(...);
builder.Services.AddIdentity<AppUsuario, IdentityRole>();
```

**Violación #2**: Controllers dependen de repositorios concretos (aunque son inyectados por interfaz, no hay capa de abstracción superior):
```csharp
public CategoriasController(ICategoriaRepositorio ctRepo, IMapper mapper)
```

**Violación #3**: No hay abstracción de servicios de infraestructura (file storage, email, etc.)

---

## 5. Deuda Técnica Identificada

### 5.1 Deuda Crítica (Debe resolverse primero)

| # | Problema | Severidad | Impacto | Archivos afectados |
|---|----------|-----------|---------|-------------------|
| 1 | **Sin capa de Dominio** | 🔴 Alta | Lógica de negocio dispersa, no testeable | `Modelos/*.cs` |
| 2 | **Sin capa de Aplicación** | 🔴 Alta | Controllers acoplados a infraestructura | `Controllers/*.cs` |
| 3 | **Lógica de archivos en controllers** | 🔴 Alta | Violación SRP, difícil de testear | `PeliculasController.cs` |
| 4 | **Single project architecture** | 🔴 Alta | Acoplamiento, imposible escalar horizontalmente | `ApiPeliculas.csproj` |

### 5.2 Deuda Media (Debe resolverse en el corto plazo)

| # | Problema | Severidad | Impacto | Archivos afectados |
|---|----------|-----------|---------|-------------------|
| 5 | `Guardar()` retorna `bool` en vez de excepciones | 🟡 Media | Manejo de errores inconsistente | `Repositorio/*.cs` |
| 6 | `ModelState` para errores de negocio | 🟡 Media | Mezcla validación de input con reglas de negocio | `Controllers/*.cs` |
| 7 | Nullable reference types deshabilitados | 🟡 Media | Riesgo de null reference exceptions | `ApiPeliculas.csproj` |
| 8 | `DateTime.Now` en vez de `DateTime.UtcNow` | 🟡 Media | Problemas de zona horaria | `Repositorio/*.cs` |
| 9 | Mapeo manual con `foreach` en controllers | 🟡 Media | Puede usar AutoMapper `ProjectTo` | `Controllers/*.cs` |
| 10 | `StatusCode(404)` para errores de validación | 🟡 Media | Código HTTP incorrecto | `Controllers/*.cs` |

### 5.3 Deuda Baja (Mejoras de calidad)

| # | Problema | Severidad | Impacto | Archivos afectados |
|---|----------|-----------|---------|-------------------|
| 11 | Sin MediatR / CQRS | 🟢 Baja | Escalabilidad limitada | Todo el proyecto |
| 12 | Sin Result Pattern | 🟢 Baja | Manejo de errores verboso | `Controllers/*.cs` |
| 13 | Sin FluentValidation | 🟢 Baja | Validación dispersa en Data Annotations | `Modelos/Dtos/*.cs` |
| 14 | Sin tests unitarios | 🟢 Baja | Sin cobertura de pruebas | Todo el proyecto |
| 15 | Sin middleware de manejo global de excepciones | 🟢 Baja | Duplicación de try-catch | `Controllers/*.cs` |

---

## 6. Análisis de Seguridad

### 6.1 Fortalezas

- ✅ JWT Bearer authentication implementado
- ✅ Roles (`Admin`, `User`) en endpoints sensibles
- ✅ CORS restrictivo (no wildcard)
- ✅ Validación de inputs con Data Annotations
- ✅ Contraseñas hasheadas por ASP.NET Identity
- ✅ Archivos renombrados con GUID

### 6.2 Debilidades

- ⚠️ `ValidateIssuer = false` y `ValidateAudience = false` en JWT
- ⚠️ `RequireHttpsMetadata = false` en desarrollo (pero podría pasar a producción)
- ⚠️ JWT Secret hardcodeado en `appsettings.json`
- ⚠️ Connection string con password hardcodeado
- ⚠️ Sin rate limiting
- ⚠️ Sin validación de tamaño de archivos en upload
- ⚠️ Sin validación de tipos MIME en upload
- ⚠️ Sin sanitización de filenames (Path Traversal risk)
- ⚠️ Logs pueden exponer PII (no hay sanitización configurada)

---

## 7. Análisis de Performance

### 7.1 Fortalezas

- ✅ Response caching global (30 segundos)
- ✅ `AsNoTracking` no se usa, pero la app es pequeña
- ✅ Paginación implementada en películas

### 7.2 Debilidades

- ⚠️ N+1 queries en `GetCategorias` (mapeo con foreach sin `Include`)
- ⚠️ Sin `IQueryable` projection (no usa `ProjectTo` de AutoMapper)
- ⚠️ Sin `CancellationToken` en métodos async
- ⚠️ `CountAsync` + `ToListAsync` no usados en repositorios
- ⚠️ Sin cache distribuida (Redis)
- ⚠️ Sin compresión de respuestas

---

## 8. Comparativa: Estado Actual vs Clean Architecture

| Característica | Estado Actual | Clean Architecture |
|----------------|---------------|-------------------|
| **Proyectos separados** | ❌ Single project | ✅ 4+ proyectos |
| **Capa Domain** | ❌ POCOs anémicos | ✅ Entidades ricas con comportamiento |
| **Capa Application** | ❌ Controllers orquestan | ✅ Services / CQRS / MediatR |
| **Capa Infrastructure** | ❌ Mezclada con API | ✅ Proyecto separado |
| **Independencia de frameworks** | ❌ EF Core en toda la app | ✅ Solo en Infrastructure |
| **Inversión de Dependencias** | ⚠️ Parcial (solo Repos) | ✅ Completa (Domain define contratos) |
| **DTOs** | ✅ Presentes | ✅ Presentes |
| **Repository Pattern** | ✅ Implementado | ✅ Implementado |
| **DI Container** | ✅ Configurado | ✅ Configurado |
| **CQRS** | ❌ No implementado | ✅ Separar Commands/Queries |
| **Result Pattern** | ❌ No implementado | ✅ Manejo de errores funcional |
| **FluentValidation** | ❌ No implementado | ✅ Validación de aplicación |
| **Domain Events** | ❌ No implementado | ✅ Desacoplamiento de lógica |
| **Unit Tests** | ❌ No hay tests | ✅ Cobertura de dominio y aplicación |

---

## 9. Recomendaciones

### 9.1 Si mantienes la arquitectura actual (optimización)

1. **Agregar un servicio de aplicación** (capa intermedia entre controller y repository)
2. **Extraer lógica de archivos** a un servicio dedicado (`IFileStorageService`)
3. **Implementar Result Pattern** para manejo de errores consistente
4. **Agregar middleware global** para manejo de excepciones
5. **Habilitar nullable reference types** (`<Nullable>enable</Nullable>`)
6. **Usar `DateTime.UtcNow`** en lugar de `DateTime.Now`
7. **Agregar tests unitarios** para repositorios y controllers
8. **Corregir códigos HTTP** (404 → 400 para errores de validación)

### 9.2 Si migras a Clean Architecture (recomendado para escalabilidad)

1. **Crear proyectos separados**:
   ```
   ApiPeliculas.Domain
   ApiPeliculas.Application
   ApiPeliculas.Infrastructure
   ApiPeliculas.API
   ```

2. **Refactorizar entidades** a Domain (POCOs → entidades ricas)

3. **Crear servicios de aplicación** con CQRS + MediatR

4. **Mover EF Core e Identity** a Infrastructure

5. **Implementar interfaces** de servicios en Application

6. **Configurar DI** con dependencias que apunten hacia adentro

7. **Agregar FluentValidation** para validación de aplicación

8. **Implementar Result Pattern** para manejo de errores

---

## 10. Métricas de Calidad

| Métrica | Valor | Benchmark |
|---------|-------|-----------|
| **Ciclomático promedio** | ~15 (controllers) | <10 ideal |
| **Líneas por método** | ~30-50 (controllers) | <20 ideal |
| **Acoplamiento aferente** | Alto | Medio ideal |
| **Acoplamiento eferente** | Alto | Medio ideal |
| **Cobertura de tests** | 0% | >80% ideal |
| **Dependencias de proyectos** | 1 | 4-5 ideal |
| **Archivos por capa** | Mezclados | Separados ideal |

---

## 11. Conclusiones

**Estado actual**: Esta es una **API REST tradicional con Repository Pattern** que funciona para un proyecto educativo o MVP. Es funcional pero no está preparada para escalabilidad, mantenimiento a largo plazo, o equipos grandes.

**Riesgo principal**: El acoplamiento entre presentación e infraestructura. Cualquier cambio en la base de datos (cambiar de SQL Server a PostgreSQL) o en el almacenamiento de archivos (local a Azure Blob Storage) requeriría modificar controllers, lo que es un claro indicador de violación de principios arquitectónicos.

**Recomendación**: Si este es un proyecto de aprendizaje, es un buen punto de partida. Si va a producción o crecerá en funcionalidad, se recomienda una refactorización gradual hacia Clean Architecture o al menos una separación de capas en proyectos distintos.

---

*Documento generado por análisis automatizado del código fuente.*
*Próxima revisión recomendada: Después de cualquier refactorización mayor.*
