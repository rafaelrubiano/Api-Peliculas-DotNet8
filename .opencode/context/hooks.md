# Hooks, Middleware & Pipelines - ApiPeliculas

> **Last updated**: 2026-06-10
> **Current state**: No custom middleware, no pipeline behaviors, no global hooks

## ASP.NET Core Middleware Pipeline

**Configured in**: `Program.cs` (lines 167-201)

**Current order**:
```
1. app.UseSwagger()
2. app.UseSwaggerUI()          [Development only]
3. app.UseStaticFiles()        # wwwroot/ImagenesPeliculas
4. app.UseHttpsRedirection()
5. app.UseCors("PoliticaCors")
6. app.UseAuthentication()
7. app.UseAuthorization()
8. app.MapControllers()
9. app.Run()
```

**Missing middleware** (not configured):
- ❌ `UseExceptionHandler` / custom exception middleware
- ❌ `UseResponseCaching` (registered in services but not added to pipeline)
- ❌ `UseRequestLogging`
- ❌ `UseHealthChecks`
- ❌ `UseRateLimiting`
- ❌ `UseHsts` (only implied by HTTPS redirection)
- ❌ `UseSecurityHeaders`

## Dependency Injection Container

**Configured in**: `Program.cs` (lines 15-165)

### Registered Services

```csharp
// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSql")));

// Identity
builder.Services.AddIdentity<AppUsuario, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Caching
builder.Services.AddResponseCaching();

// Repositories (Scoped)
builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<IPeliculaRepositorio, PeliculaRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(PeliculasMapper));

// API Versioning
builder.Services.AddApiVersioning(options => {
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});
builder.Services.AddApiExplorer(options => {
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// JWT Authentication
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// Controllers with Cache Profile
builder.Services.AddControllers(options => {
    options.CacheProfiles.Add("PorDefecto30Segundos", new CacheProfile { Duration = 30 });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => { ... });

// CORS
builder.Services.AddCors(options => {
    options.AddPolicy("PoliticaCors", builder => {
        builder.WithOrigins("http://localhost:5103")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

## Controller Pipeline Attributes

### Global/Controller-level Filters

- `[ApiController]` - Automatic model validation, binding source inference
- `[Route("api/v{version:apiVersion}/...")]` - Versioned routing
- `[ApiVersion("1.0")]` / `[ApiVersion("2.0")]` - Version declaration
- `[ApiVersionNeutral]` - Version-independent (UsuariosController)

### Action-level Filters

**Authentication**:
- `[AllowAnonymous]` - Skip authentication (GET public endpoints, registro, login)
- `[Authorize(Roles = "Admin")]` - Require Admin role (POST, PUT, PATCH, DELETE)

**Caching**:
- `[ResponseCache(CacheProfileName = "PorDefecto30Segundos")]` - 30s cache

**ProducesResponseType**:
- Declares expected status codes for Swagger documentation

**Obsolete**:
- `[Obsolete("Use la versión 2")]` - Marks `GetString` endpoint as deprecated

## Model Binding

**Sources used**:
- `[FromBody]` - JSON payload (CrearCategoriaDto, CategoriaDto, UsuarioRegistroDto, UsuarioLoginDto)
- `[FromForm]` - Form data + file upload (CrearPeliculaDto, ActualizarPeliculaDto)
- `[FromQuery]` - Query parameters (pageNumber, pageSize)
- `[FromRoute]` - URL segments (id parameters, implicit via route template)

## Validation Pipeline

**Current**: Data Annotations + ModelState

**Annotations used**:
- `[Required]` - Categoria.Nombre, CategoriaDto.Nombre, CrearCategoriaDto.Nombre, UsuarioLoginDto.*, UsuarioRegistroDto.*
- `[MaxLength(100)]` - CategoriaDto.Nombre, CrearCategoriaDto.Nombre
- `[Key]` - Entity IDs
- `[ForeignKey]` - Pelicula.categoriaId

**Validation flow**:
```
Request → Model Binding → Data Annotations → ModelState.IsValid → Controller logic
```

**Missing**:
- ❌ FluentValidation pipeline
- ❌ Custom validation attributes
- ❌ Cross-property validation
- ❌ Async validation

## No Cross-Cutting Concerns Implemented

The following patterns are NOT present:

- **No MediatR pipelines** (no behaviors for logging, validation, caching, transactions)
- **No Action filters** (no logging filter, no audit filter, no transaction filter)
- **No Result filters** (no response transformation)
- **No Exception filters** (no global exception handling)
- **No Authorization policies** (only role-based, no custom policies)
- **No Resource filters** (no request throttling at filter level)

## Swagger Pipeline Configuration

**Security definition**:
```csharp
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
    Description = "JWT Bearer auth. Enter: Bearer {token}",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Scheme = "Bearer"
});
```

**Security requirement**:
Applied globally to all endpoints (Swagger UI will prompt for token).

## Recommended Hooks for Clean Architecture

When migrating, add:

1. **ExceptionMiddleware** - Global exception handling, standardized error responses
2. **ValidationBehavior** (MediatR) - Centralize FluentValidation
3. **LoggingBehavior** (MediatR) - Log all commands/queries
4. **TransactionBehavior** (MediatR) - Wrap commands in transactions
5. **PerformanceBehavior** (MediatR) - Log slow queries
6. **RequestLoggingMiddleware** - Log all HTTP requests
7. **SecurityHeadersMiddleware** - Add security headers
8. **RateLimitingMiddleware** - Prevent abuse

## Current Pipeline Diagram

```
HTTP Request
    ↓
[Swagger] (if dev)
    ↓
[Static Files]
    ↓
[HTTPS Redirect]
    ↓
[CORS]
    ↓
[Authentication]
    ↓
[Authorization]
    ↓
[Model Binding]
    ↓
[Data Annotations Validation]
    ↓
[Controller Action]
    ↓
[Repository]
    ↓
[EF Core / SQL Server]
    ↓
HTTP Response
```

**No interceptors, no decorators, no global hooks.**
