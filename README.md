# ApiPeliculas - .NET 8 RESTful API

Api RESTful desarrollada en **.NET 8** para la gestión de películas y categorías, con autenticación JWT, almacenamiento de imágenes, paginación y versionamiento de APIs.

## Características Principales

- Gestión completa de **Películas** y **Categorías** (CRUD)
- **Autenticación JWT** con ASP.NET Core Identity
- **Subida de imágenes** para películas con almacenamiento local
- **Paginación** en listado de películas
- **Búsqueda** de películas por nombre
- **Filtrado** de películas por categoría
- **Versionamiento de API** (v1 y v2)
- **Response Caching** global (30 segundos)
- **CORS** configurado para desarrollo local
- **Swagger/OpenAPI** con documentación de endpoints y autenticación Bearer

## Stack Tecnológico

- **.NET 8** (Web API)
- **Entity Framework Core 8.0.4** + SQL Server
- **ASP.NET Core Identity** (gestión de usuarios y roles)
- **JWT Bearer Authentication**
- **AutoMapper** (mapeo de entidades a DTOs)
- **Asp.Versioning.Mvc** (versionamiento de API)
- **Swashbuckle.AspNetCore** (Swagger/OpenAPI)
- **Docker** (con soporte para Linux)

## Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (versión 8.0.0 o superior)
- [SQL Server](https://www.microsoft.com/sql-server) (local o instancia de Docker)
- (Opcional) [Docker](https://www.docker.com/) y Docker Compose

## Instalación y Configuración

### 1. Clonar el repositorio

```bash
git clone <url-del-repositorio>
cd ApiPeliculas
```

### 2. Configurar la base de datos

La connection string está configurada en `ApiPeliculas/appsettings.json` apuntando a `localhost`:

```json
"ConnectionStrings": {
  "ConexionSql": "Data Source=localhost;TrustServerCertificate=True;MultiSubnetFailover=True;Initial Catalog=ApiPeliculasNET8;user id=sa;password=r34llyStr0ngPwd123"
}
```

Asegúrate de que SQL Server esté corriendo y accesible. Si usas Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=r34llyStr0ngPwd123" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### 3. Aplicar migraciones

```bash
dotnet ef database update --project ApiPeliculas
```

### 4. Ejecutar la API

```bash
dotnet run --project ApiPeliculas --launch-profile http
```

La API estará disponible en:
- **Swagger UI**: `http://localhost:5103/swagger`
- **API Base**: `http://localhost:5103`

## Estructura del Proyecto

```
ApiPeliculas/
├── ApiPeliculas/
│   ├── Controllers/
│   │   ├── V1/              # Controladores versión 1
│   │   │   ├── CategoriasController.cs
│   │   │   └── PeliculasController.cs
│   │   ├── V2/              # Controladores versión 2
│   │   │   └── CategoriasController.cs
│   │   └── UsuariosController.cs
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Modelos/
│   │   ├── Categoria.cs
│   │   ├── Pelicula.cs
│   │   ├── Usuario.cs
│   │   ├── AppUsuario.cs
│   │   ├── RespuestaAPI.cs
│   │   └── Dtos/            # Data Transfer Objects
│   │       ├── CategoriaDto.cs
│   │       ├── CrearCategoriaDto.cs
│   │       ├── PeliculaDto.cs
│   │       ├── CrearPeliculaDto.cs
│   │       ├── ActualizarPeliculaDto.cs
│   │       ├── UsuarioDto.cs
│   │       ├── UsuarioDatosDto.cs
│   │       ├── UsuarioLoginDto.cs
│   │       ├── UsuarioLoginRespuestaDto.cs
│   │       └── UsuarioRegistroDto.cs
│   ├── Repositorio/
│   │   ├── IRepositorio/    # Interfaces
│   │   └── *.cs             # Implementaciones
│   ├── PeliculasMappers/
│   │   └── PeliculasMapper.cs
│   ├── Migrations/          # Migraciones de EF Core
│   ├── wwwroot/
│   │   └── ImagenesPeliculas/  # Almacenamiento de imágenes
│   ├── Program.cs
│   ├── appsettings.json
│   └── ApiPeliculas.csproj
├── ApiPeliculas.sln
├── Dockerfile
├── compose.yaml
└── global.json
```

## Documentación de la API

### Versionamiento

La API soporta versionamiento mediante URL:

- **v1 (default)**: `api/v1.0/...`
- **v2**: `api/v2.0/...`

### Endpoints

#### Autenticación

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| `POST` | `/api/v{version}/usuarios/registro` | Registro de nuevo usuario | Público |
| `POST` | `/api/v{version}/usuarios/login` | Login y obtención de JWT | Público |

**Registro (Request):**
```json
{
  "nombreUsuario": "johndoe",
  "nombre": "John Doe",
  "password": "SecureP@ss123",
  "role": "User"
}
```

**Login (Request):**
```json
{
  "nombreUsuario": "johndoe",
  "password": "SecureP@ss123"
}
```

**Login (Response):**
```json
{
  "statusCode": 200,
  "isSuccess": true,
  "result": {
    "usuario": { "id": "...", "username": "johndoe", "nombre": "John Doe" },
    "role": "User",
    "token": "eyJhbGciOiJIUzI1NiIs..."
  }
}
```

#### Categorías (v1)

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| `GET` | `/api/v1.0/categorias` | Listar todas las categorías | Público |
| `GET` | `/api/v1.0/categorias/{id}` | Obtener categoría por ID | Público |
| `POST` | `/api/v1.0/categorias` | Crear nueva categoría | Admin |
| `PUT` | `/api/v1.0/categorias/{id}` | Actualizar categoría (completo) | Admin |
| `PATCH` | `/api/v1.0/categorias/{id}` | Actualizar categoría (parcial) | Admin |
| `DELETE` | `/api/v1.0/categorias/{id}` | Eliminar categoría | Admin |

#### Categorías (v2)

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| `GET` | `/api/v2.0/categorias` | Endpoint de prueba v2 | Público |

#### Películas (v1)

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| `GET` | `/api/v1.0/peliculas?pageNumber=1&pageSize=2` | Listar películas paginadas | Público |
| `GET` | `/api/v1.0/peliculas/{id}` | Obtener película por ID | Público |
| `POST` | `/api/v1.0/peliculas` | Crear película (con imagen) | Admin |
| `PATCH` | `/api/v1.0/peliculas/{id}` | Actualizar película (con imagen) | Admin |
| `DELETE` | `/api/v1.0/peliculas/{id}` | Eliminar película | Admin |
| `GET` | `/api/v1.0/peliculas/GetPeliculasEnCategoria/{categoriaId}` | Filtrar por categoría | Público |
| `GET` | `/api/v1.0/peliculas/Buscar?nombre=termino` | Buscar películas por nombre | Público |

**Paginación (Response):**
```json
{
  "pageNumber": 1,
  "pageSize": 2,
  "totalPages": 5,
  "totalItems": 10,
  "items": [ /* PeliculaDto[] */ ]
}
```

#### Usuarios

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| `GET` | `/api/v{version}/usuarios` | Listar usuarios | Admin |
| `GET` | `/api/v{version}/usuarios/{id}` | Obtener usuario por ID | Admin |

### Autenticación en Swagger

Para probar endpoints protegidos en Swagger:
1. Haz login mediante `/api/v1.0/usuarios/login`
2. Copia el token del campo `result.token`
3. En Swagger, haz clic en **Authorize** (candado)
4. Ingresa: `Bearer <tu-token>`
5. Los endpoints protegidos estarán disponibles

### Respuestas de Error

La API utiliza `RespuestaAPI` para respuestas estandarizadas:

```json
{
  "statusCode": 400,
  "isSuccess": false,
  "errorMessages": ["El nombre de usuario ya existe"],
  "result": null
}
```

## Manejo de Imágenes

- Las imágenes se suben mediante **multipart/form-data** (`IFormFile`)
- Almacenamiento local en `wwwroot/ImagenesPeliculas/`
- Nombres de archivo generados con **GUID** para evitar colisiones
- URLs de imagen accesibles públicamente: `http://localhost:5103/ImagenesPeliculas/{nombre}.png`
- Si no se sube imagen, se asigna una imagen placeholder (`https://placehold.co/600x400`)

## Seguridad

- **JWT Bearer** para autenticación stateless
- **Roles** (`Admin`, `User`) para autorización basada en claims
- **CORS** restringido a `http://localhost:5103` en desarrollo
- **Response Caching** de 30 segundos para endpoints GET públicos
- Validación de inputs con **Data Annotations** y **ModelState**
- La clave JWT (`ApiSettings:Secreta`) se lee desde `appsettings.json`

> ⚠️ **Nota**: El proyecto está configurado para desarrollo local. En producción, usa Azure Key Vault o variables de entorno para secrets y connection strings.

## Docker

El repositorio incluye un `Dockerfile` en la raíz que:
- Compila la aplicación en .NET 8 SDK
- Publica en `/app/publish`
- Expone los puertos `8080` y `8081`

> ⚠️ **Nota**: El archivo `compose.yaml` referencia `ApiPeliculas/Dockerfile`, pero el Dockerfile actual está en la raíz del repo. Para usar Docker Compose, actualiza `compose.yaml`:
> ```yaml
> dockerfile: Dockerfile
> ```

## Convenciones del Proyecto

- **Nullable** deshabilitado (`<Nullable>disable</Nullable>`)
- **ImplicitUsings** habilitado
- Nombres de DTOs en español (ej: `CrearCategoriaDto`, `PeliculaDto`)
- Controladores organizados por versión en `Controllers/V1/` y `Controllers/V2/`
- Repositorios implementan **Repository Pattern** con interfaces en `IRepositorio/`
- AutoMapper centralizado en `PeliculasMapper`

## Contribuir

1. Crea una rama feature: `git checkout -b feature/nueva-funcionalidad`
2. Realiza tus cambios siguiendo las convenciones del proyecto
3. Ejecuta la aplicación y verifica en Swagger
4. Envía un Pull Request

## Licencia

Desarrollo de Software - Codex-io

---

**Desarrollado con .NET 8** | **API Version: v1.0 / v2.0**
