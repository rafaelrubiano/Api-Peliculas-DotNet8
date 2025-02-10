using Microsoft.Build.ObjectModelRemoting;

namespace ApiPeliculas.Modelos.Dtos;

public class CrearPeliculaDto
{
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public int Duracion { get; set; }
    public string? RutaImagen { get; set; }
    public IFormFile Imagen { get; set; }

    public enum CrearTipoClasificacion
    {
        Siete,
        Trece,
        Diesciseis,
        Diesciocho
    }
    public CrearTipoClasificacion Clasificacion { get; set; }
    public int categoriaId { get; set; }
}