# ApiPeliculas - .NET 8 RESTful API

Api RESTful desarrollada en **.NET 8** para la gestión de películas y categorías, con autenticación JWT, almacenamiento de imágenes, paginación y versionamiento de APIs.

## Arquitectura del Proyecto

```mermaid
graph TB
    subgraph Client["🌐 Cliente"]
        Swagger["Swagger UI / Postman"]
        WebApp["Web App"]
    end

    subgraph Api["📦 ApiPeliculas API"]
        direction TB
        
        Controllers["🎮 Controllers<br/>V1/V2 + Auth"]
        Repositories["📦 Repositories<br/>Categoria/Pelicula/Usuario"]
        Models["📋 Models<br/>Entities + DTOs"]
        Mapper["🔄 AutoMapper<br/>PeliculasMapper"]
        
        subgraph CrossCutting["⚙️ Cross-Cutting"]
            Serilog["🔍 Serilog<br/>Structured Logging"]
            JWT["🔐 JWT Auth<br/>Bearer + Identity"]
            CORS["🔗 CORS<br/>localhost:5103"]
            Cache["💾 Cache<br/>30s Profile"]
            Versioning["📌 API Versioning<br/>v1/v2"]
        end
    end

    subgraph Database["🗄️ Database"]
        SQLServer["SQL Server<br/>ApiPeliculasNET8"]
        Identity["Identity Tables<br/>AspNetUsers/Roles"]
    end

    subgraph Storage["📁 Storage"]
        Images["wwwroot/ImagenesPeliculas/"]
    end

    subgraph DevOps["🐳 DevOps"]
        Docker["Docker Compose<br/>API + SQL Server"]
    end

    Swagger -->|HTTP| Controllers
    WebApp -->|HTTP| Controllers
    
    Controllers -->|uses| JWT
    Controllers -->|uses| CORS
    Controllers -->|uses| Cache
    Controllers -->|uses| Versioning
    Controllers -->|uses| Repositories
    Controllers -->|uses| Mapper
    Controllers -->|uses| Serilog
    
    Repositories -->|CRUD| Models
    Mapper -->|maps| Models
    
    Repositories -->|EF Core| SQLServer
    Repositories -->|Identity| Identity
    
    Controllers -->|File Upload| Images
    
    Docker -->|orchestrates| Api
    Docker -->|orchestrates| Database

    style Api fill:#e1f5fe,stroke:#01579b,stroke-width:2px
    style CrossCutting fill:#fff3e0,stroke:#e65100,stroke-width:2px
    style Database fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
```

> 💡 **Tip:** GitHub renderiza automáticamente los diagramas Mermaid. Si estás en VS Code, usa la extensión [Markdown Preview Mermaid Support](https://marketplace.visualstudio.com/items?itemName=bierner.markdown-mermaid) para verlo en la preview.

## Diagramas del Proyecto

### Diagrama 2: Arquitectura Target (Clean Architecture)

```mermaid
graph LR
    subgraph Domain["🏛️ Domain Layer"]
        Entities["Entities<br/>Categoria, Pelicula, Usuario"]
        ValueObjects["Value Objects"]
        DomainEvents["Domain Events"]
        Interfaces["Repository Interfaces"]
    end

    subgraph Application["📋 Application Layer"]
        UseCases["Use Cases / CQRS<br/>Commands & Queries"]
        DTOs["DTOs<br/>Request/Response"]
        Validators["Validators<br/>FluentValidation"]
        InterfacesApp["Service Interfaces"]
    end

    subgraph Infrastructure["🔧 Infrastructure Layer"]
        Repositories["Repositories<br/>EF Core Implementation"]
        Identity["Identity<br/>ASP.NET Core"]
        Storage["File Storage<br/>Local/Azure Blob"]
        Email["Email Service"]
    end

    subgraph API["🌐 API Layer"]
        Controllers["Controllers<br/>V1/V2"]
        Middleware["Middleware<br/>Auth/Logging/Error"]
        Filters["Filters<br/>Validation"]
    end

    subgraph External["🔗 External Services"]
        DB["SQL Server"]
        AuthProvider["Auth Provider"]
        CloudStorage["Cloud Storage"]
    end

    API -->|depends on| Application
    Application -->|depends on| Domain
    Infrastructure -->|depends on| Domain
    Infrastructure -->|depends on| Application
    
    Infrastructure -->|uses| DB
    Infrastructure -->|uses| AuthProvider
    Infrastructure -->|uses| CloudStorage
    
    style Domain fill:#e8f5e9,stroke:#1b5e20,stroke-width:3px
    style Application fill:#e3f2fd,stroke:#0d47a1,stroke-width:2px
    style Infrastructure fill:#fff3e0,stroke:#e65100,stroke-width:2px
    style API fill:#fce4ec,stroke:#c62828,stroke-width:2px
```

> 🎯 **Target**: Arquitectura limpia con separación de responsabilidades (Domain → Application → Infrastructure → API). Dependencias siempre hacia adentro.

### Diagrama 3: Docker Compose (Desarrollo)

```mermaid
graph TB
    subgraph DockerHost["🐳 Docker Host"]
        
        subgraph Network["📡 apipeliculas-network<br/>(bridge)"]
            
            subgraph SQLService["🗄️ sqlserver"]
                SQLImage["mcr.microsoft.com/mssql/server:2022-latest"]
                SQLPort["Port: 1433"]
                SQLVolume["Volume:<br/>sqlserver_data"]
                SQLHealth["Health Check:<br/>sqlcmd -Q SELECT 1"]
            end
            
            subgraph APIService["📦 apipeliculas"]
                APIBuild["Build:<br/>Dockerfile"]
                APIPort["Port: 5103 → 8080"]
                APIEnv["Env:<br/>ASPNETCORE_ENVIRONMENT=Development"]
                APISecrets["Secrets:<br/>JWT + ConnectionString"]
                APIDepends["depends_on:<br/>sqlserver (healthy)"]
            end
            
        end
        
    end

    Client["👤 Developer"] -->|localhost:5103| APIService
    Client -->|localhost:1433| SQLService
    
    APIService -->|Data Source=sqlserver| SQLService
    
    style SQLService fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    style APIService fill:#e3f2fd,stroke:#0d47a1,stroke-width:2px
```

> 🐳 **Desarrollo**: Orquestación con SQL Server 2022 + API .NET 8. Health checks aseguran que SQL Server esté listo antes de iniciar la API.

### Diagrama 4: Flow de una Request (Autenticada)

```mermaid
sequenceDiagram
    autonumber
    participant Client as Cliente
    participant API as API Controller
    participant Auth as JWT Middleware
    participant Log as Serilog
    participant Repo as Repository
    participant DB as SQL Server
    participant Cache as Response Cache

    Client->>+API: POST /api/v1.0/categorias
    Note right of Client: Bearer <token>
    
    API->>+Auth: Validar JWT
    Auth-->>-API: Usuario: Admin
    Note right of Auth: ValidateIssuerSigningKey<br/>Role: Admin
    
    API->>+Log: LogInformation("Creando categoría...")
    
    API->>+Repo: CrearCategoria(categoria)
    Repo->>+DB: INSERT INTO Categorias
    DB-->>-Repo: Id = 1
    Repo-->>-API: true
    
    API->>+Log: LogInformation("Categoría creada: {Id}")
    
    API->>Cache: Invalidate Cache (si aplica)
    
    API-->>-Client: 201 Created<br/>Location: /api/v1.0/categorias/1
    
    Note right of API: Response:<br/>{ id: 1, nombre: "Acción" }
```

> 🔐 **Autenticación**: JWT Bearer → Validación de roles → Logging → Repository → Cache → Response. Cada paso se registra en Serilog.

### Diagrama 5: Security & Secrets Hierarchy

```mermaid
graph LR
    subgraph ConfigHierarchy["🏛️ Configuration Hierarchy<br/>Priority: Low → High"]
        direction TB
        
        Appsettings["1️⃣ appsettings.json<br/>Placeholders / Defaults"]
        DevSettings["2️⃣ appsettings.Development.json<br/>Dev Overrides"]
        UserSecrets["3️⃣ User Secrets<br/>🔒 Sensitive Data"]
        EnvVars["4️⃣ Environment Variables<br/>🔒 Docker / Production"]
        CLIArgs["5️⃣ CLI Arguments<br/>Runtime Overrides"]
        
        Appsettings -->|overridden by| DevSettings
        DevSettings -->|overridden by| UserSecrets
        UserSecrets -->|overridden by| EnvVars
        EnvVars -->|overridden by| CLIArgs
    end

    subgraph Secrets["🔐 Secrets Managed"]
        JWT["JWT Signing Key<br/>ApiSettings:Secreta"]
        DBConn["Connection String<br/>ConnectionStrings:ConexionSql"]
    end

    subgraph Development["💻 Development"]
        DevLocal["User Secrets Store<br/>~/.microsoft/usersecrets/"]
    end

    subgraph Docker["🐳 Docker"]
        DockerEnv[".env file<br/>compose.yaml"]
    end

    subgraph Production["☁️ Production"]
        Azure["Azure Key Vault"]
        AWS["AWS Secrets Manager"]
    end

    UserSecrets -->|stores| JWT
    UserSecrets -->|stores| DBConn
    DevLocal -->|contains| UserSecrets
    
    EnvVars -->|stores| JWT
    EnvVars -->|stores| DBConn
    DockerEnv -->|contains| EnvVars
    
    Azure -->|stores| JWT
    Azure -->|stores| DBConn
    AWS -->|stores| JWT
    AWS -->|stores| DBConn
    
    style UserSecrets fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    style EnvVars fill:#e3f2fd,stroke:#0d47a1,stroke-width:2px
    style Secrets fill:#fff3e0,stroke:#e65100,stroke-width:2px
```

> 🔐 **Jerarquía**: Configuración en cascada. User Secrets en desarrollo, Environment Variables en Docker, Azure Key Vault / AWS Secrets Manager en producción.

### Diagrama 6: CI/CD Pipeline (Target)

```mermaid
graph LR
    subgraph Dev["👨‍💻 Developer"]
        Code["Code + Tests"]
        PR["Pull Request"]
    end

    subgraph CI["🔄 CI Pipeline"]
        Build["dotnet build"]
        Test["dotnet test<br/>13 tests"]
        Lint["dotnet format"]
        Security["Security Scan"]
    end

    subgraph CD["🚀 CD Pipeline"]
        DockerBuild["docker build"]
        Push["Push to Registry"]
        Deploy["Deploy to Cloud"]
    end

    subgraph Cloud["☁️ Cloud"]
        AzureContainer["Azure Container Apps"]
        AWSContainer["AWS ECS/Fargate"]
        Monitoring["Application Insights<br/>CloudWatch"]
    end

    Code --> PR
    PR --> Build
    Build --> Test
    Test --> Lint
    Lint --> Security
    Security --> DockerBuild
    DockerBuild --> Push
    Push --> Deploy
    Deploy --> AzureContainer
    Deploy --> AWSContainer
    AzureContainer --> Monitoring
    AWSContainer --> Monitoring

    style CI fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    style CD fill:#e3f2fd,stroke:#0d47a1,stroke-width:2px
    style Cloud fill:#fce4ec,stroke:#c62828,stroke-width:2px
```

> 🚀 **CI/CD**: Build → Test (13 unit tests) → Security Scan → Docker Build → Push Registry → Deploy Azure/AWS → Monitor.

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
- **Serilog** + **Serilog.AspNetCore** (logging estructurado con soporte JSON para cloud)
- **xUnit** + **Moq** (unit testing)
- **Docker** + **Docker Compose** (con soporte para Linux)

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

### 2. Configurar Secrets (User Secrets)

Los secrets sensibles (JWT key y connection string) **no están hardcodeados** en el código. Se gestionan mediante **ASP.NET Core User Secrets**:

```bash
# Verificar que User Secrets esté inicializado (ya configurado en el proyecto)
dotnet user-secrets list --project ApiPeliculas/ApiPeliculas.csproj

# Si necesitas configurarlos por primera vez:
dotnet user-secrets set "ApiSettings:Secreta" "tu-jwt-secret" --project ApiPeliculas/ApiPeliculas.csproj
dotnet user-secrets set "ConnectionStrings:ConexionSql" "Data Source=localhost;TrustServerCertificate=True;MultiSubnetFailover=True;Initial Catalog=ApiPeliculasNET8;user id=sa;password=r34llyStr0ngPwd123" --project ApiPeliculas/ApiPeliculas.csproj
```

### 3. Configurar la base de datos

Asegúrate de que SQL Server esté corriendo y accesible. Si usas Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=r34llyStr0ngPwd123" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### 4. Aplicar migraciones

```bash
dotnet ef database update --project ApiPeliculas
```

### 5. Ejecutar la API

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
│   ├── appsettings.Development.json
│   └── ApiPeliculas.csproj
├── ApiPeliculas.Tests/      # Unit Tests (xUnit + Moq)
│   └── Controllers/
│       ├── CategoriasControllerTests.cs
│       ├── PeliculasControllerTests.cs
│       └── UsuariosControllerTests.cs
├── ApiPeliculas.sln
├── Dockerfile
├── compose.yaml
├── .env.example
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

## Unit Tests

El proyecto incluye **13 unit tests** con **xUnit** y **Moq** para los 3 controllers principales:

| Controller | Tests | Escenarios |
|-----------|-------|------------|
| `CategoriasController` | 4 | Get lista, get by ID, lista vacía, not found |
| `PeliculasController` | 5 | Paginación, not found, excepción, get by ID |
| `UsuariosController` | 4 | Registro exitoso, duplicado, fallido, excepción |

### Ejecutar tests

```bash
# Ejecutar todos los tests
dotnet test ApiPeliculas.Tests/ApiPeliculas.Tests.csproj

# Con verbose
dotnet test ApiPeliculas.Tests/ApiPeliculas.Tests.csproj --verbosity normal
```

### Patrón de testing

- **Mocking** de repositorios con `Mock<T>` (sin base de datos real)
- **AAA Pattern** (Arrange-Act-Assert)
- Cobertura de happy path, validaciones de negocio, edge cases y excepciones

## Logging Estructurado (Serilog)

La API implementa **logging estructurado** con **Serilog** para observabilidad cloud-native:

### Características

- **Formato JSON** en producción (compatible con AWS CloudWatch, Azure Monitor)
- **Formato legible** en desarrollo
- **Request logging** automático (tiempo, status code, método, path)
- **Enriquecimiento** de logs con `Application`, `Environment`, `MachineName`
- **Captura de excepciones** con stack trace completo en todos los catch blocks

### Ejemplo de log en producción (JSON)

```json
{
  "@t": "2026-06-10T20:15:30.123Z",
  "@l": "Error",
  "@m": "Error al recuperar películas. PageNumber=1, PageSize=10",
  "PageNumber": 1,
  "PageSize": 10,
  "Application": "ApiPeliculas",
  "Environment": "Production"
}
```

### Niveles de log implementados

| Nivel | Uso | Ejemplo |
|-------|-----|---------|
| `LogInformation` | Operaciones exitosas | "Película creada: {Id}" |
| `LogWarning` | Eventos esperados (404, duplicados) | "Película no encontrada: {Id}" |
| `LogError` | Excepciones y errores | "Error al recuperar películas: {Page}" |

## Docker Compose (Desarrollo Completo)

El entorno de desarrollo incluye **SQL Server + API** en Docker Compose:

### Iniciar

```bash
# Copiar variables de entorno
cp .env.example .env

# Iniciar SQL Server + API
docker compose up -d

# Aplicar migraciones (primera vez)
docker compose exec apipeliculas dotnet ef database update --project ApiPeliculas
```

### Servicios

| Servicio | Puerto | Descripción |
|----------|--------|-------------|
| `sqlserver` | `1433` | SQL Server 2022 (Developer) |
| `apipeliculas` | `5103` | API .NET 8 + Swagger |

### Comandos útiles

```bash
# Ver logs
docker compose logs -f apipeliculas
docker compose logs -f sqlserver

# Detener
docker compose down

# Limpiar todo (incluyendo datos)
docker compose down -v
```

> **Nota**: `compose.yaml` usa `dockerfile: Dockerfile` (raíz del repo). Verifica que apunte correctamente.

## Seguridad

- **JWT Bearer** para autenticación stateless
- **Roles** (`Admin`, `User`) para autorización basada en claims
- **CORS** restringido a `http://localhost:5103` en desarrollo
- **Response Caching** de 30 segundos para endpoints GET públicos
- Validación de inputs con **Data Annotations** y **ModelState**
- **Secrets** gestionados via **User Secrets** (desarrollo) o **Variables de Entorno** (producción)
- **NO** hay secrets hardcodeados en `appsettings.json` (valores vacíos/placeholders)

### Configuración de CORS

```csharp
// Solo permite el origen de desarrollo
builder.Services.AddCors(p => p.AddPolicy("PoliticaCors", build =>
{
    build.WithOrigins("http://localhost:5103")
         .AllowAnyMethod()
         .AllowAnyHeader();
}));
```

### Gestión de Secrets

| Entorno | Método | Archivo |
|---------|--------|---------|
| **Desarrollo** | User Secrets | Almacenado localmente en `~/.microsoft/usersecrets/` |
| **Docker** | Variables de entorno | `.env` o `compose.yaml` |
| **Producción** | Azure Key Vault / AWS Secrets Manager | Configurado en CI/CD |

> ⚠️ **Nota**: El proyecto está configurado para desarrollo local. En producción, usa Azure Key Vault o AWS Secrets Manager para secrets y connection strings.

## 🤖 Development with AI Tools

Este proyecto fue desarrollado aprovechando **OpenCode** (agentes de IA personalizados) para acelerar la implementación y mejorar la calidad:

- **Testing**: 13 unit tests con xUnit/Moq implementados en tiempo reducido
- **Logging**: Configuración profesional de Serilog (JSON, enriquecimiento, request tracking)
- **Infrastructure**: Docker Compose con health checks y service orchestration
- **Security**: Secrets management (User Secrets + environment variables)
- **Documentation**: Contexto del proyecto y guías arquitectónicas generadas automáticamente

Ver [`AI_development_workflow.md`](./AI_development_workflow.md) para detalles específicos sobre:
- Cómo se usó OpenCode en cada componente
- Validación y testing de outputs generados por IA
- Impacto en productividad y calidad (66% más rápido)

**Enfoque:** IA como amplificador de habilidades técnicas, no como reemplazo. El agente arquitecto toma decisiones de diseño y el agente desarrollador ejecuta implementaciones, con validación humana en cada paso.

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
