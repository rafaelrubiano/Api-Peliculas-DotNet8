using ApiPeliculas.Data;
using ApiPeliculas.Modelos;
using ApiPeliculas.Repositorio.IRepositorio;

namespace ApiPeliculas.Repositorio;

public class CategoriaRepositorio : ICategoriaRepositorio
{
    private readonly ApplicationDbContext _bd;

    public CategoriaRepositorio(ApplicationDbContext bd)
    {
        _bd = bd;
    }

    public bool ActualizarCategoria(Categoria categoria)
    {
        categoria.FechaCreacion = DateTime.Now;
        
        // Arreglar problema con el PUT
        var categoriaExistente = _bd.Categoria.Find(categoria.Id);
        if (categoriaExistente != null)
        {
            _bd.Entry(categoriaExistente).CurrentValues.SetValues(categoria);
        }
        else
        {
            _bd.Categoria.Update(categoria);
        }
        return Guardar();
    }
    
    public bool BorrarCategoria(Categoria categoria)
    {
        _bd.Categoria.Remove(categoria);
        return Guardar();
    }
    
    public bool CrearCategoria(Categoria categoria)
    {
        categoria.FechaCreacion = DateTime.Now;
        _bd.Categoria.Add(categoria);
        return Guardar();
    }
    
    public bool ExisteCategoria(int id)
    {
        return _bd.Categoria.Any(c => c.Id == id);
    }
    
    public bool ExisteCategoria(string nombre)
    {
        bool valor = _bd.Categoria.Any(c => c.Nombre.ToLower().Trim() == nombre.ToLower().Trim());
        return valor;
    }
    
    public Categoria GetCategoria(int CategoriaId)
    {
        return _bd.Categoria.FirstOrDefault(c => c.Id == CategoriaId);
    }
    
    public ICollection<Categoria> GetCategorias()
    {
        return _bd.Categoria.OrderBy(c => c.Nombre).ToList();
    }

    public bool Guardar()
    {
        return _bd.SaveChanges() >= 0 ? true : false;
    }
}