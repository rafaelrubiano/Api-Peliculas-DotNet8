using System.Security.Principal;

namespace ApiPeliculas.Modelos;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; }
    public string Nombre { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
}