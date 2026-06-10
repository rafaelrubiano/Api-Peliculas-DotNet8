# ApiPeliculas - Architecture Diagrams

## Diagrama 1: Arquitectura Actual (Monolito N-Layer)

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

---

## Diagrama 2: Arquitectura Target (Clean Architecture)

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

---

## Diagrama 3: Docker Compose (Desarrollo)

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

---

## Diagrama 4: Flow de una Request (Autenticada)

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

---

## Diagrama 5: Security & Secrets Hierarchy

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

---

## Diagrama 6: CI/CD Pipeline (Target)

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

---

## Cómo usar estos diagramas

### En VS Code con "Diagrams Previewer"

1. **Instala la extensión**:
   - Busca `Markdown Preview Mermaid Support` en VS Code Marketplace
   - O `Markdown PDF` (incluye Mermaid)
   - O `Diagrams Previewer` (si soporta Mermaid)

2. **Abre este archivo** en VS Code:
   ```
   .opencode/docs/diagrams.md
   ```

3. **Abre la preview**:
   - `Ctrl+Shift+V` (Windows/Linux)
   - `Cmd+Shift+V` (Mac)
   - O clic derecho → "Open Preview"

4. **Los diagramas se renderizan automáticamente** como SVG

### En GitHub

GitHub soporta Mermaid nativamente en Markdown:
- Solo sube este archivo a un repo
- Los diagramas se renderizan automáticamente en la vista web

### Exportar como imagen

Para exportar como PNG/PDF:
```bash
# Usando Mermaid CLI
npm install -g @mermaid-js/mermaid-cli
mmdc -i diagrams.md -o output.pdf
```

---

## Notas

- **Mermaid** es el estándar de facto para diagramas en Markdown
- **Sintaxis**: Similar a PlantUML pero más simple
- **Tipos de diagramas**: Flowchart, Sequence, Gantt, Class, State, ER, User Journey
- **Customización**: Colores, estilos, themes via CSS

---

*Diagramas generados por: OpenCode (Agente Arquitecto)*
*Fecha: 2026-06-10*
*Formato: Mermaid (compatible con VS Code, GitHub, GitLab)*
