# Unit Tests - ApiPeliculas

Documento de referencia con 3 unit tests de ejemplo para el proyecto ApiPeliculas. Estos tests demuestran patrones de testing para controllers, lógica de negocio y validación, usando xUnit + Moq + AutoMapper.

---

## 1. Test para CategoriasController.GetCategorias

### Archivo: `Tests/CategoriasControllerTests.cs`

```csharp
using Xunit;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ApiPeliculas.Controllers.V1;
using ApiPeliculas.Modelos;
using ApiPeliculas.Modelos.Dtos;
using ApiPeliculas.Repositorio.IRepositorio;
using System.Collections.Generic;
using System.Linq;

public class CategoriasControllerTests
{
    private readonly Mock<ICategoriaRepositorio> _mockRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CategoriasController _controller;

    public CategoriasControllerTests()
    {
        _mockRepo = new Mock<ICategoriaRepositorio>();
        _mockMapper = new Mock<IMapper>();
        _controller = new CategoriasController(_mockRepo.Object, _mockMapper.Object);
    }

    [Fact]
    public void GetCategorias_RetornaOkConListaDeCategorias()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new Categoria { Id = 1, Nombre = "Acción", Estado = "Activo" },
            new Categoria { Id = 2, Nombre = "Comedia", Estado = "Activo" }
        };
        var categoriasDto = new List<CategoriaDto>
        {
            new CategoriaDto { Id = 1, Nombre = "Acción", Estado = "Activo" },
            new CategoriaDto { Id = 2, Nombre = "Comedia", Estado = "Activo" }
        };

        _mockRepo.Setup(r => r.GetCategorias()).Returns(categorias);
        _mockMapper.Setup(m => m.Map<CategoriaDto>(It.IsAny<Categoria>()))
            .Returns((Categoria c) => categoriasDto.First(d => d.Id == c.Id));

        // Act
        var result = _controller.GetCategorias();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<List<CategoriaDto>>(okResult.Value);
        Assert.Equal(2, returnValue.Count);
        Assert.Equal("Acción", returnValue[0].Nombre);
        Assert.Equal("Comedia", returnValue[1].Nombre);
    }

    [Fact]
    public void GetCategorias_CuandoListaVacia_RetornaOkConListaVacia()
    {
        // Arrange
        var categorias = new List<Categoria>();
        var categoriasDto = new List<CategoriaDto>();

        _mockRepo.Setup(r => r.GetCategorias()).Returns(categorias);
        _mockMapper.Setup(m => m.Map<CategoriaDto>(It.IsAny<Categoria>()))
            .Returns((Categoria c) => null);

        // Act
        var result = _controller.GetCategorias();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<List<CategoriaDto>>(okResult.Value);
        Assert.Empty(returnValue);
    }
}
```

### Qué se valida:
- ✅ Retorna `OkObjectResult` (HTTP 200)
- ✅ La lista de DTOs tiene la cantidad correcta de elementos
- ✅ El mapping se ejecuta correctamente
- ✅ Caso edge: lista vacía retorna OK con lista vacía (no 404)

### Dependencias mockeadas:
- `ICategoriaRepositorio` - retorna datos de prueba
- `IMapper` - simula el mapeo Entity -> DTO

---

## 2. Test para PeliculasController.GetPeliculas (Paginación V2)

### Archivo: `Tests/PeliculasControllerTests.cs`

```csharp
using Xunit;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ApiPeliculas.Controllers.V1;
using ApiPeliculas.Modelos;
using ApiPeliculas.Modelos.Dtos;
using ApiPeliculas.Repositorio.IRepositorio;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;

public class PeliculasControllerTests
{
    private readonly Mock<IPeliculaRepositorio> _mockRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PeliculasController _controller;

    public PeliculasControllerTests()
    {
        _mockRepo = new Mock<IPeliculaRepositorio>();
        _mockMapper = new Mock<IMapper>();
        _controller = new PeliculasController(_mockRepo.Object, _mockMapper.Object);
    }

    [Fact]
    public void GetPeliculas_ConPaginacion_RetornaOkConMetadataDePaginacion()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 2;
        var totalPeliculas = 5;
        var peliculas = new List<Pelicula>
        {
            new Pelicula { Id = 1, Nombre = "Pelicula 1", CategoriaId = 1 },
            new Pelicula { Id = 2, Nombre = "Pelicula 2", CategoriaId = 1 }
        };
        var peliculasDto = new List<PeliculaDto>
        {
            new PeliculaDto { Id = 1, Nombre = "Pelicula 1" },
            new PeliculaDto { Id = 2, Nombre = "Pelicula 2" }
        };

        _mockRepo.Setup(r => r.GetTotalPeliculas()).Returns(totalPeliculas);
        _mockRepo.Setup(r => r.GetPeliculas(pageNumber, pageSize)).Returns(peliculas);
        _mockMapper.Setup(m => m.Map<PeliculaDto>(It.IsAny<Pelicula>()))
            .Returns((Pelicula p) => peliculasDto.First(d => d.Id == p.Id));

        // Act
        var result = _controller.GetPeliculas(pageNumber, pageSize);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        // Usar reflection para verificar propiedades anónimas
        var responseType = response.GetType();
        var pageNumberProp = responseType.GetProperty("pageNumber");
        var pageSizeProp = responseType.GetProperty("pageSize");
        var totalPagesProp = responseType.GetProperty("TotalPages");
        var totalItemsProp = responseType.GetProperty("TotalItems");
        var itemsProp = responseType.GetProperty("Items");

        Assert.NotNull(pageNumberProp);
        Assert.NotNull(pageSizeProp);
        Assert.NotNull(totalPagesProp);
        Assert.NotNull(totalItemsProp);
        Assert.NotNull(itemsProp);

        Assert.Equal(1, pageNumberProp.GetValue(response));
        Assert.Equal(2, pageSizeProp.GetValue(response));
        Assert.Equal(3, totalPagesProp.GetValue(response)); // Math.Ceiling(5/2)
        Assert.Equal(5, totalItemsProp.GetValue(response));
        
        var items = itemsProp.GetValue(response) as List<PeliculaDto>;
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void GetPeliculas_SinResultados_RetornaNotFound()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 2;
        var peliculas = new List<Pelicula>();

        _mockRepo.Setup(r => r.GetTotalPeliculas()).Returns(0);
        _mockRepo.Setup(r => r.GetPeliculas(pageNumber, pageSize)).Returns(peliculas);

        // Act
        var result = _controller.GetPeliculas(pageNumber, pageSize);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("No se encontraron películas.", notFoundResult.Value);
    }

    [Fact]
    public void GetPeliculas_ExcepcionEnRepositorio_Retorna500()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetTotalPeliculas())
            .Throws(new System.Exception("Error de base de datos"));

        // Act
        var result = _controller.GetPeliculas(1, 2);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains("Error recuperando datos", statusCodeResult.Value.ToString());
    }
}
```

### Qué se valida:
- ✅ Respuesta paginada con metadata correcta (pageNumber, pageSize, TotalPages, TotalItems)
- ✅ Cálculo de `TotalPages` = `Math.Ceiling(totalItems / pageSize)`
- ✅ Retorna 404 cuando no hay películas
- ✅ Retorna 500 cuando hay excepción en el repositorio
- ✅ Uso de reflection para testear objetos anónimos (tipo `new { ... }`)

### Patrón destacado:
```csharp
// El controller retorna un objeto anónimo:
new { pageNumber, pageSize, TotalPages, TotalItems, Items }

// En tests se usa reflection para acceder las propiedades:
var responseType = response.GetType();
var pageNumberProp = responseType.GetProperty("pageNumber");
```

---

## 3. Test para UsuariosController.Registro (Validación + Auth)

### Archivo: `Tests/UsuariosControllerTests.cs`

```csharp
using Xunit;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ApiPeliculas.Controllers;
using ApiPeliculas.Modelos;
using ApiPeliculas.Modelos.Dtos;
using ApiPeliculas.Repositorio.IRepositorio;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;

public class UsuariosControllerTests
{
    private readonly Mock<IUsuarioRepositorio> _mockRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly UsuariosController _controller;

    public UsuariosControllerTests()
    {
        _mockRepo = new Mock<IUsuarioRepositorio>();
        _mockMapper = new Mock<IMapper>();
        _controller = new UsuariosController(_mockRepo.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Registro_UsuarioUnico_RetornaCreatedConUsuario()
    {
        // Arrange
        var registroDto = new UsuarioRegistroDto
        {
            NombreUsuario = "nuevo_usuario",
            Nombre = "Nuevo",
            Password = "password123",
            Role = "User"
        };

        var usuarioCreado = new UsuarioDatosDto
        {
            Id = "1",
            NombreUsuario = "nuevo_usuario",
            Nombre = "Nuevo"
        };

        _mockRepo.Setup(r => r.IsUniqueUser(registroDto.NombreUsuario)).Returns(true);
        _mockRepo.Setup(r => r.Registro(registroDto)).ReturnsAsync(usuarioCreado);

        // Act
        var result = await _controller.Registro(registroDto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var respuesta = Assert.IsType<RespuestaAPI>(createdResult.Value);
        
        Assert.True(respuesta.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty(respuesta.ErrorMessages);
        Assert.NotNull(respuesta.Result);
        
        var usuarioRespuesta = Assert.IsType<UsuarioDatosDto>(respuesta.Result);
        Assert.Equal("nuevo_usuario", usuarioRespuesta.NombreUsuario);
    }

    [Fact]
    public async Task Registro_UsuarioDuplicado_RetornaBadRequest()
    {
        // Arrange
        var registroDto = new UsuarioRegistroDto
        {
            NombreUsuario = "usuario_existente",
            Nombre = "Existente",
            Password = "password123",
            Role = "User"
        };

        _mockRepo.Setup(r => r.IsUniqueUser(registroDto.NombreUsuario)).Returns(false);

        // Act
        var result = await _controller.Registro(registroDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var respuesta = Assert.IsType<RespuestaAPI>(badRequestResult.Value);
        
        Assert.False(respuesta.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Single(respuesta.ErrorMessages);
        Assert.Contains("nombre de usuario ya existe", respuesta.ErrorMessages[0]);
        Assert.Null(respuesta.Result);
        
        // Verificar que nunca se llamó a Registro() porque falló la validación
        _mockRepo.Verify(r => r.Registro(It.IsAny<UsuarioRegistroDto>()), Times.Never);
    }

    [Fact]
    public async Task Registro_RegistroFallido_RetornaBadRequest()
    {
        // Arrange
        var registroDto = new UsuarioRegistroDto
        {
            NombreUsuario = "nuevo_usuario",
            Nombre = "Nuevo",
            Password = "password123",
            Role = "User"
        };

        _mockRepo.Setup(r => r.IsUniqueUser(registroDto.NombreUsuario)).Returns(true);
        _mockRepo.Setup(r => r.Registro(registroDto)).ReturnsAsync((UsuarioDatosDto)null);

        // Act
        var result = await _controller.Registro(registroDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var respuesta = Assert.IsType<RespuestaAPI>(badRequestResult.Value);
        
        Assert.False(respuesta.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Error en el registro", respuesta.ErrorMessages[0]);
    }

    [Fact]
    public async Task Registro_ExcepcionEnRegistro_Retorna500()
    {
        // Arrange
        var registroDto = new UsuarioRegistroDto
        {
            NombreUsuario = "nuevo_usuario",
            Nombre = "Nuevo",
            Password = "password123",
            Role = "User"
        };

        _mockRepo.Setup(r => r.IsUniqueUser(registroDto.NombreUsuario)).Returns(true);
        _mockRepo.Setup(r => r.Registro(registroDto))
            .ThrowsAsync(new System.Exception("Error de conexión a base de datos"));

        // Act
        var result = await _controller.Registro(registroDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var respuesta = Assert.IsType<RespuestaAPI>(statusCodeResult.Value);
        
        Assert.False(respuesta.IsSuccess);
        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains("Error de conexión", respuesta.ErrorMessages[0]);
    }
}
```

### Qué se valida:
- ✅ **Happy path**: Usuario único, registro exitoso -> `Created` (201) con datos del usuario
- ✅ **Validación de negocio**: Usuario duplicado -> `BadRequest` (400) sin llamar al repositorio
- ✅ **Registro fallido**: Repositorio retorna null -> `BadRequest` (400) con mensaje de error
- ✅ **Excepción**: Error en repositorio -> `500` con mensaje de excepción
- ✅ **Verificación de comportamiento**: `_mockRepo.Verify(..., Times.Never)` para confirmar que no se ejecutó registro cuando falló validación

### Patrón de validación de negocio testeado:
```csharp
// El controller tiene esta validación:
bool validarNombreUsuarioUnico = _usRepo.IsUniqueUser(usuarioRegistroDto.NombreUsuario);
if (!validarNombreUsuarioUnico)
{
    return BadRequest(_respuestaApi); // Early return
}
// Solo si pasa validación, llama a Registro()
```

---

## Configuración del Proyecto de Tests

### 1. Crear proyecto de tests
```bash
dotnet new xunit -n ApiPeliculas.Tests -o Tests
```

### 2. Agregar referencias
```bash
dotnet add Tests/ApiPeliculas.Tests.csproj reference ApiPeliculas/ApiPeliculas.csproj
```

### 3. Paquetes NuGet necesarios
```bash
dotnet add Tests/ApiPeliculas.Tests.csproj package Moq --version 4.20.70
dotnet add Tests/ApiPeliculas.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
```

### 4. Estructura de carpetas sugerida
```
ApiPeliculas.Tests/
├── Controllers/
│   ├── CategoriasControllerTests.cs    # Test 1
│   ├── PeliculasControllerTests.cs     # Test 2
│   └── UsuariosControllerTests.cs    # Test 3
├── Repositorios/
│   └── (tests de repositorios con InMemory DB)
├── Helpers/
│   └── TestDbContextFactory.cs
└── ApiPeliculas.Tests.csproj
```

### 5. Registrar en la solución
```bash
dotnet sln add Tests/ApiPeliculas.Tests.csproj
```

---

## Buenas Prácticas Aplicadas

### AAA Pattern (Arrange-Act-Assert)
Todos los tests siguen la estructura:
```csharp
// Arrange - Preparar datos y mocks
// Act - Ejecutar el método a testear
// Assert - Verificar resultados
```

### Naming Convention
```csharp
[Metodo]_[Escenario]_[ResultadoEsperado]

// Ejemplos:
GetCategorias_RetornaOkConListaDeCategorias
Registro_UsuarioDuplicado_RetornaBadRequest
GetPeliculas_ExcepcionEnRepositorio_Retorna500
```

### Mocking de Dependencias
- **Nunca** se usa la base de datos real en unit tests
- **Siempre** se mockean repositorios con `Mock<T>`
- Se usa `Setup()` para definir comportamiento y `Verify()` para confirmar llamadas

### Coverage de Casos
Cada test suite cubre:
- ✅ Happy path (escenario exitoso)
- ✅ Validación de negocio (datos inválidos/duplicados)
- ✅ Edge cases (listas vacías, nulls)
- ✅ Excepciones (errores de infraestructura)

### Isolation
- Cada test es independiente
- No comparten estado entre tests
- Constructor inicializa mocks fresco para cada test

---

## Notas de Implementación

### Nullable Disabled
El proyecto tiene `<Nullable>disable</Nullable>`, por lo que no se requiere manejo de nullables en los tests. Los mocks pueden retornar `null` sin warning.

### AutoMapper en Tests
Para tests más robustos, considerar usar `MapperConfiguration` real en lugar de mocks:
```csharp
var config = new MapperConfiguration(cfg => cfg.AddProfile<PeliculasMapper>());
var mapper = config.CreateMapper();
```
Esto testea el mapping real, pero es más lento. Los mocks son más rápidos para unit tests puros.

### Testing de Claims/Auth
Para testear endpoints con `[Authorize]`, usar `DefaultHttpContext` con claims:
```csharp
var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
{
    new Claim(ClaimTypes.Role, "Admin")
}, "mock"));
_controller.ControllerContext = new ControllerContext
{
    HttpContext = new DefaultHttpContext { User = user }
};
```

### Testing de IFormFile
Para testear upload de imágenes (CrearPelicula/ActualizarPatchPelicula):
```csharp
var fileMock = new Mock<IFormFile>();
fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
fileMock.Setup(f => f.FileName).Returns("test.jpg");
fileMock.Setup(f => f.Length).Returns(1024);
```

---

## Resumen

| Test | Controller | Escenarios Cubiertos | Técnicas |
|------|-----------|---------------------|----------|
| 1 | CategoriasController.GetCategorias | OK, lista vacía | Mock repo + mapper |
| 2 | PeliculasController.GetPeliculas | OK con paginación, 404, 500 | Reflection para objetos anónimos |
| 3 | UsuariosController.Registro | OK, duplicado, fallido, excepción | Verify de no-ejecución, async tests |

Estos 3 tests sirven como base para expandir la cobertura del proyecto a todos los endpoints y capas.
