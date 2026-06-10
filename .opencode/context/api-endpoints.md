# API Endpoints - ApiPeliculas

> **Last updated**: 2026-06-10
> **Base URL**: `http://localhost:5103`
> **Versioning**: URL-based (`api/v{version}`)

## Authentication

### POST /api/v{version}/usuarios/registro
**Access**: Public (`AllowAnonymous`)
**Body**:
```json
{
  "nombreUsuario": "johndoe",
  "nombre": "John Doe",
  "password": "SecureP@ss123",
  "role": "User"
}
```
**Response** (201 Created):
```json
{
  "statusCode": 200,
  "isSuccess": true,
  "errorMessages": [],
  "result": {
    "usuario": { "id": "...", "userName": "johndoe", "nombre": "John Doe" },
    "role": "User",
    "token": "eyJhbGciOiJIUzI1NiIs..."
  }
}
```

### POST /api/v{version}/usuarios/login
**Access**: Public (`AllowAnonymous`)
**Body**:
```json
{
  "nombreUsuario": "johndoe",
  "password": "SecureP@ss123"
}
```
**Response** (200 OK):
```json
{
  "statusCode": 200,
  "isSuccess": true,
  "errorMessages": [],
  "result": {
    "usuario": { "id": "...", "userName": "johndoe", "nombre": "John Doe" },
    "role": "User",
    "token": "eyJhbGciOiJIUzI1NiIs..."
  }
}
```

## Categorías (v1)

### GET /api/v1.0/categorias
**Access**: Public (`AllowAnonymous`)
**Cache**: `PorDefecto30Segundos` (30s)
**Response** (200 OK):
```json
[
  { "id": 1, "nombre": "Acción", "fechaCreacion": "2024-01-01T00:00:00" }
]
```

### GET /api/v1.0/categorias/{id}
**Access**: Public (`AllowAnonymous`)
**Cache**: `PorDefecto30Segundos` (30s)
**Response** (200 OK) or 404 Not Found

### POST /api/v1.0/categorias
**Access**: Admin only (`[Authorize(Roles = "Admin")]`)
**Body**: `CrearCategoriaDto` (JSON)
```json
{ "nombre": "Nueva Categoría" }
```
**Validation**: `MaxLength(100)`, `Required`
**Response**: 201 Created with `Location` header

### PUT /api/v1.0/categorias/{id}
**Access**: Admin only
**Body**: `CategoriaDto` (JSON)
```json
{ "id": 1, "nombre": "Categoría Actualizada", "fechaCreacion": "..." }
```
**Response**: 204 NoContent or 400/404

### PATCH /api/v1.0/categorias/{id}
**Access**: Admin only
**Body**: `CategoriaDto` (JSON)
**Response**: 204 NoContent or 400/404

### DELETE /api/v1.0/categorias/{id}
**Access**: Admin only
**Response**: 204 NoContent or 404

### GET /api/v1.0/categorias/GetString
**Access**: Public (no auth attribute)
**Status**: `[Obsolete("Use la versión 2")]`
**Response**: `["valor1", "valor2", "valor3"]`

## Categorías (v2)

### GET /api/v2.0/categorias
**Access**: Public (no auth attribute)
**Response**: `["valor1", "valor2", "valor3"]`

## Películas (v1)

### GET /api/v1.0/peliculas
**Access**: Public (`AllowAnonymous`)
**Query params**: `?pageNumber=1&pageSize=2`
**Response** (200 OK):
```json
{
  "pageNumber": 1,
  "pageSize": 2,
  "totalPages": 5,
  "totalItems": 10,
  "items": [
    { "id": 1, "nombre": "Película 1", "descripcion": "...", "duracion": 120, "clasificacion": "Trece", "categoriaId": 1 }
  ]
}
```

### GET /api/v1.0/peliculas/{id}
**Access**: Public (`AllowAnonymous`)
**Response**: 200 OK or 404 Not Found

### POST /api/v1.0/peliculas
**Access**: Admin only (`[Authorize(Roles = "Admin")]`)
**Content-Type**: `multipart/form-data` (`[FromForm]`)
**Fields**:
- `nombre` (string)
- `descripcion` (string)
- `duracion` (int)
- `clasificacion` (string: Siete/Trece/Diesciseis/Diesciocho)
- `categoriaId` (int)
- `imagen` (IFormFile, optional)

**Image handling**:
- If image provided: saves to `wwwroot/ImagenesPeliculas/{guid}{ext}`, returns URL
- If no image: returns `https://placehold.co/600x400`

**Response**: 201 Created

### PATCH /api/v1.0/peliculas/{id}
**Access**: Admin only
**Content-Type**: `multipart/form-data` (`[FromForm]`)
**Fields**: Same as POST + `id` (must match URL)
**Response**: 204 NoContent

### DELETE /api/v1.0/peliculas/{id}
**Access**: Admin only
**Response**: 204 NoContent

### GET /api/v1.0/peliculas/GetPeliculasEnCategoria/{categoriaId}
**Access**: Public (`AllowAnonymous`)
**Response**: 200 OK with array of `PeliculaDto` or 404 Not Found

### GET /api/v1.0/peliculas/Buscar?nombre={term}
**Access**: Public (`AllowAnonymous`)
**Response**: 200 OK with array of `PeliculaDto` or 404 Not Found

## Usuarios

### GET /api/v{version}/usuarios
**Access**: Admin only (`[Authorize(Roles = "Admin")]`)
**Response**: 200 OK with array of `UsuarioDto`

### GET /api/v{version}/usuarios/{id}
**Access**: Admin only
**Cache**: `PorDefecto30Segundos` (30s)
**Response**: 200 OK or 404 Not Found

## Status Codes Summary

| Code | Meaning | Usage |
|------|---------|-------|
| 200 | OK | Successful GET, PUT, PATCH |
| 201 | Created | Successful POST (with Location header for categorías/películas) |
| 204 | NoContent | Successful DELETE, PUT, PATCH |
| 400 | BadRequest | Validation errors, ModelState invalid |
| 401 | Unauthorized | Missing/invalid JWT token |
| 403 | Forbidden | Valid token but insufficient role |
| 404 | NotFound | Resource not found (sometimes used for validation errors in current code) |
| 500 | InternalServerError | Unhandled exceptions (generic catch blocks) |

## Authentication Header

All protected endpoints require:
```
Authorization: Bearer {jwt_token}
```

Configure in Swagger UI via the "Authorize" button (lock icon).

## Response Caching

Cached endpoints (30 seconds):
- `GET /api/v1.0/categorias`
- `GET /api/v1.0/categorias/{id}`
- `GET /api/v{version}/usuarios/{id}`

## Notes

- **No HATEOAS**: Responses do not include links.
- **No Content Negotiation**: Only JSON supported.
- **No API rate limiting**: Unlimited requests.
- **Inconsistent status codes**: Some validation errors return 404 instead of 400.
- **No standardized error format**: Mix of `ModelState`, `RespuestaAPI`, and plain strings.
