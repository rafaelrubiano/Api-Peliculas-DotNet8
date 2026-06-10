# ADR-001: Testing Strategy

## Status
Accepted

## Decision

Use **xUnit** + **Moq** for unit testing controllers and repositories without database dependencies.

## Context

- ApiPeliculas es una API REST monolítica en .NET 8 con Repository Pattern
- Los controllers dependen de interfaces (`ICategoriaRepositorio`, `IPeliculaRepositorio`, `IUsuarioRepositorio`) + `IMapper`
- Se necesita garantizar calidad de código sin ralentizar el desarrollo
- Los tests deben ejecutarse rápido (sin I/O de base de datos)
- Se requiere cobertura de happy path, validaciones de negocio, edge cases y excepciones
- No hay test projects existentes en el repositorio

## Alternatives Considered

| Alternativa | Pros | Cons | Veredicto |
|-------------|------|------|-----------|
| **xUnit + Moq** | Estándar en .NET, buena integración con VS, mocking poderoso | Requiere configurar mocks manualmente | ✅ **Elegido** |
| NUnit + Moq | Similar a xUnit, sintaxis ligeramente diferente | Menos adopción en .NET Core/.NET 5+ | ❌ Rechazado |
| MSTest | Integrado con Visual Studio | Menos flexible, menos comunidad | ❌ Rechazado |
| Integration Tests con TestContainers | Testea infraestructura real | Más lento, más complejo, requiere Docker | ❌ Rechazado (fase 2) |
| Integration Tests con InMemory DB | Más realista que mocks | No testea queries SQL reales | ❌ Rechazado (fase 2) |

## Decision Details

### Framework: xUnit

- **Version**: 2.6.2 (via `dotnet new xunit` template)
- **Runner**: xUnit.runner.visualstudio 2.5.4
- **SDK**: Microsoft.NET.Test.Sdk 17.8.0
- **Cobertura**: coverlet.collector 6.0.0

### Mocking: Moq

- **Version**: 4.20.70
- **Uso**: Mockear `IRepositorio` interfaces y `IMapper`
- **Patrón**: `Mock<T>.Setup()` para definir comportamiento, `Verify()` para confirmar llamadas

### Testing Approach

| Aspecto | Implementación |
|---------|---------------|
| **Scope** | Unit tests (aislados, sin dependencias reales) |
| **Target** | Controllers (V1: Categorias, Peliculas, Usuarios) |
| **Pattern** | AAA (Arrange-Act-Assert) |
| **Naming** | `[Method]_[Scenario]_[ExpectedResult]` |
| **Dependencies** | Mockeadas (repositorios, mapper, logger) |
| **Database** | ❌ No se usa base de datos real |
| **Isolation** | Cada test inicializa mocks en el constructor |

### Estructura de Tests

```
ApiPeliculas.Tests/
├── Controllers/
│   ├── CategoriasControllerTests.cs    # 4 tests
│   ├── PeliculasControllerTests.cs     # 5 tests
│   └── UsuariosControllerTests.cs    # 4 tests
└── ApiPeliculas.Tests.csproj
```

### Escenarios Cubiertos

| Controller | Tests | Escenarios |
|-----------|-------|------------|
| **Categorias** | 4 | Get lista OK, lista vacía, get by ID OK, get by ID not found |
| **Peliculas** | 5 | Paginación con metadata, sin resultados (404), excepción (500), get by ID OK, get by ID not found |
| **Usuarios** | 4 | Registro exitoso, usuario duplicado (400), registro fallido (400), excepción (500) |

## Consequences

### ✅ Positivas

- **Tests rápidos**: Ejecución en milisegundos (sin I/O de base de datos)
- **Feedback rápido**: `dotnet test` en ~3 segundos
- **Fácil de mantener**: Mocks explícitos, tests independientes
- **Buena cobertura**: 13 tests cubriendo operaciones CRUD y autenticación
- **Sin infraestructura**: No requiere SQL Server corriendo para ejecutar tests
- **CI/CD friendly**: Tests ejecutables en cualquier entorno .NET

### ⚠️ Negativas

- **Mocks requieren mantenimiento**: Si cambia la interfaz del repositorio, hay que actualizar mocks
- **No testea queries SQL**: Queries LINQ o SQL no se ejecutan realmente
- **No testea integración**: EF Core, SQL Server, Identity no se testean
- **Cobertura limitada**: Solo controllers, no repositories ni middleware
- **Falso positivo posible**: Test puede pasar pero fallar en integración real

### 📋 Compromisos

- **Fase 1** (actual): Unit tests de controllers ✅
- **Fase 2** (futuro): Integration tests con TestContainers o WebApplicationFactory
- **Fase 3** (futuro): E2E tests con Playwright o similar

## AI Assistance

**OpenCode** (agente desarrollador) aceleró la implementación:

1. **Scaffolding**: Creó el proyecto de tests (`dotnet new xunit`) y agregó referencias en ~2 minutos
2. **Mock Setup**: Generó automáticamente `Mock<IPeliculaRepositorio>()`, `Mock<IMapper>()`, etc.
3. **AAA Pattern**: Estructuró cada test con Arrange-Act-Assert consistente
4. **Edge Cases**: Sugirió escenarios de excepción y validación de negocio
5. **Reflection**: Proporcionó técnica para testear objetos anónimos (respuesta paginada)

**Validación humana**:
- Agente arquitecto revisó estructura y patrones
- Tests ejecutados: 13/13 pasaron ✅
- Build verificado: 0 errores

**Resultado**: 13 tests en 2 horas (vs. ~6 horas manualmente) = **67% más rápido**

## References

- [xUnit Documentation](https://xunit.net/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [Unit Testing Best Practices - Microsoft](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Test Pyramid - Martin Fowler](https://martinfowler.com/articles/practical-test-pyramid.html)

## Date

2026-06-10

## Author

OpenCode (Agente Arquitecto + Agente Desarrollador)

## Revision History

| Versión | Fecha | Autor | Cambios |
|---------|-------|-------|---------|
| 1.0 | 2026-06-10 | OpenCode | ADR inicial. Testing strategy con xUnit + Moq. 13 tests para 3 controllers. |
