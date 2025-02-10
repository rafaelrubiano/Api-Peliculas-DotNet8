using System.ComponentModel.DataAnnotations;

namespace ApiPeliculas.Modelos.Dtos;

public class UsuarioRegistroDto
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    public string NombreUsuario { get; set; }
    public string Nombre { get; set; }
    [Required(ErrorMessage = "El password es obligatorio")]
    public string Password { get; set; }
    public string Role { get; set; }
}