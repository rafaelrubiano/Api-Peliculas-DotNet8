# Repository Structure - ApiPeliculas

> **Last updated**: 2026-06-10
> **Project**: ApiPeliculas (.NET 8 RESTful API)
> **Single-project solution** (not Clean Architecture yet)

## Root Layout

```
ApiPeliculas/
├── ApiPeliculas.sln                    # Solution file (single project)
├── global.json                          # SDK policy: 8.0.0, latestMajor, allowPrerelease
├── Dockerfile                           # Root-level Dockerfile (Linux target)
├── compose.yaml                         # Docker Compose (references ApiPeliculas/Dockerfile - does NOT exist)
├── .dockerignore                        # Docker ignore rules
├── .gitignore                           # Standard .NET gitignore
├── README.md                            # Project documentation (recently updated)
├── AGENTS.md                            # Agent guidance for OpenCode
└── ApiPeliculas/                        # Single project folder
    ├── ApiPeliculas.csproj              # Project file (.NET 8 Web SDK)
    ├── ApiPeliculas.http                # HTTP test file (localhost:5103)
    ├── Program.cs                       # Application entry point, DI container, middleware pipeline
    ├── appsettings.json                 # Config: JWT secret, SQL connection string, logging
    ├── appsettings.Development.json     # Dev-specific logging config
    ├── Properties/
    │   └── launchSettings.json          # Launch profiles: http (5103), https (7155), IIS Express
    ├── Controllers/                     # API controllers grouped by version
    │   ├── V1/
    │   │   ├── CategoriasController.cs  # 8 endpoints: GET, GET/{id}, POST, PUT, PATCH, DELETE, GET/GetString (obsolete)
    │   │   └── PeliculasController.cs   # 7 endpoints: GET (paginated), GET/{id}, POST, PATCH, DELETE, GET/GetPeliculasEnCategoria, GET/Buscar
    │   ├── V2/
    │   │   └── CategoriasController.cs  # 1 endpoint: GET (returns string array)
    │   └── UsuariosController.cs        # 4 endpoints: GET, GET/{id}, POST/registro, POST/login
    ├── Data/
    │   └── ApplicationDbContext.cs      # IdentityDbContext<AppUsuario> with DbSets for Categoria, Pelicula, Usuario, AppUsuario
    ├── Modelos/                         # Domain models + DTOs (mixed in same folder)
    │   ├── Categoria.cs                 # Entity: Id, Nombre, FechaCreacion
    │   ├── Pelicula.cs                  # Entity: Id, Nombre, Descripcion, Duracion, RutaImagen, RutaLocalImagen, Clasificacion, FechaCreacion, categoriaId, Categoria
    │   ├── Usuario.cs                   # Entity: Id, NombreUsuario, Nombre, Password, Role
    │   ├── AppUsuario.cs              # IdentityUser extension: Nombre
    │   ├── RespuestaAPI.cs            # Generic API response wrapper: StatusCode, IsSuccess, ErrorMessages, Result
    │   └── Dtos/                        # Data Transfer Objects (10 files)
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
    ├── Repositorio/                     # Repository implementations + interfaces
    │   ├── IRepositorio/
    │   │   ├── ICategoriaRepositorio.cs # 7 methods: GetCategorias, GetCategoria, ExisteCategoria (x2), Crear, Actualizar, Borrar, Guardar
    │   │   ├── IPeliculaRepositorio.cs  # 10+ methods: CRUD + GetPeliculas(page, size), GetTotalPeliculas, GetPeliculasEnCategoria, BuscarPelicula
    │   │   └── IUsuarioRepositorio.cs   # 5 methods: GetUsuarios, GetUsuario, IsUniqueUser, Registro, Login
    │   ├── CategoriaRepositorio.cs
    │   ├── PeliculaRepositorio.cs
    │   └── UsuarioRepositorio.cs
    ├── PeliculasMappers/
    │   └── PeliculasMapper.cs           # AutoMapper Profile: Categoria, Pelicula, Usuario, AppUsuario mappings
    ├── Migrations/                      # EF Core migrations (8 migration files)
    │   ├── 20241127185718_MigracionInicial.cs
    │   ├── 20241215011900_Cambio de plurar a singular en la tabla Categoria.cs
    │   ├── 20241221151832_CrearTablaPelicula.cs
    │   ├── 20241222203351_CreacionTablaUsuario.cs
    │   ├── 20241231011930_AgregadoSoporteIdentity.cs
    │   ├── 20241231203057_SoporteParaSubidaImagenPelicula.cs
    │   └── ApplicationDbContextModelSnapshot.cs
    ├── wwwroot/
    │   └── ImagenesPeliculas/          # Static file storage for uploaded movie images
    ├── bin/                             # Build output (ignored)
    └── obj/                             # Object files (ignored)
```

## Key Observations

- **Single-project architecture**: All code (Domain, Application, Infrastructure, API) lives in one `.csproj`.
- **No test projects**: Zero test coverage.
- **No separation of concerns**: Models, DTOs, Repositories, Controllers all in one project.
- **Migrations exist**: 6 historical migrations from Nov 2024 to Dec 2024.
- **Docker mismatch**: `Dockerfile` at root, but `compose.yaml` references `ApiPeliculas/Dockerfile` (does not exist).
