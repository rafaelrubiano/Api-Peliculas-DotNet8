using System.Text;
using ApiPeliculas.Data;
using ApiPeliculas.Modelos;
using ApiPeliculas.PeliculasMapper;
using ApiPeliculas.Repositorio;
using ApiPeliculas.Repositorio.IRepositorio;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSql")));


//Soporte para autenticación con .Net Identity
builder.Services.AddIdentity<AppUsuario, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();


//Soporte para cache
builder.Services.AddResponseCaching();

//Agregamos los Repositorios
builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<IPeliculaRepositorio, PeliculaRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();

var key = builder.Configuration.GetValue<string>("ApiSettings:Secreta");

//Soporte para versionamiento
var apiVersioningBuilder = builder.Services.AddApiVersioning(opcion =>
{
    opcion.AssumeDefaultVersionWhenUnspecified = true;
    opcion.DefaultApiVersion = new ApiVersion(1, 0);
    opcion.ReportApiVersions = true;
    // opcion.ApiVersionReader = ApiVersionReader.Combine(
    //     new QueryStringApiVersionReader("api-version") //?api-version=1.0
    //     // new HeaderApiVersionReader("X-version"),
    //     // new MediaTypeApiVersionReader("ver");
    // );
});

apiVersioningBuilder.AddApiExplorer(
    opciones =>
    {
        opciones.GroupNameFormat = "'v'VVV";
        opciones.SubstituteApiVersionInUrl = true;
    });

//Agregamos el AutoMapper
builder.Services.AddAutoMapper(typeof(PeliculasMapper));

//Aquí se configura la Autenticación
builder.Services.AddAuthentication
(
    x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }
).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false
    };

});

builder.Services.AddControllers(opcion =>
{
    //Cache profile. Un cache global y así no tener que ponerlo en todas partes
    opcion.CacheProfiles.Add("PorDefecto30Segundos", new CacheProfile(){Duration = 30});
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = 
                "Autenticación JWT usando el esquema Bearer. \r\n\r\n " +
                "Ingresa la palabra 'Bearer' seguido de un [espacio] y después su token en el campo de abajo. \r\n\r\n " +
                "Ejemplo: \"Bearer tkljk125jhhk\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Scheme = "Bearer"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header
                },
                new List<string>()
            }
        });
        options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1.0",
                Title = "Peliculas Api V1",
                Description = "Api de Peliculas Versión 1",
                TermsOfService = new Uri("https://google.com"),
                Contact = new OpenApiContact
                {
                    Name = "Codex-io",
                    Url = new Uri("https://google.com")
                },
                License = new OpenApiLicense
                {
                    Name = "Desarrollo de Software",
                    Url = new Uri("https://google.com")
                }
            }
        );
        options.SwaggerDoc("v2", new OpenApiInfo
            {
                Version = "v2.0",
                Title = "Peliculas Api V2",
                Description = "Api de Peliculas Versión 2",
                TermsOfService = new Uri("https://google.com"),
                Contact = new OpenApiContact
                {
                    Name = "Codex-io",
                    Url = new Uri("https://google.com")
                },
                License = new OpenApiLicense
                {
                    Name = "Desarrollo de Software",
                    Url = new Uri("https://google.com")
                }
            }
        );
    }
);

//Soporte para CROS
//Se puede habilitar: 1-Un dominio, 2-multiples dominios,
//3-cualquier dominio (Tener en cuenta seguridad)
//Usamos de ejemplo el dominio: http://localhost:3223, se debe cambiar por el correcto
//Se usa (*) para todos los dominios
builder.Services.AddCors(p => p.AddPolicy("PoliticaCors", build =>
{
    build.WithOrigins("http://localhost:5103").AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();
app.UseSwagger();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(opciones =>
    {
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiPeliculasV1");
        opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "ApiPeliculasV2");
    });
}
else
{
    app.UseSwaggerUI(opciones =>
    {
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiPeliculasV1");
        opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "ApiPeliculasV2");
        opciones.RoutePrefix = "";
    });
}

//Soporte para archivos estáticos como imágenes
app.UseStaticFiles();

app.UseHttpsRedirection();

//Soporte para CORS
app.UseCors("PoliticaCors");

//Soorte para Autenticación
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();