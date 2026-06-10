using Xunit;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiPeliculas.Controllers.V1;
using ApiPeliculas.Modelos;
using ApiPeliculas.Modelos.Dtos;
using ApiPeliculas.Repositorio.IRepositorio;
using System.Collections.Generic;
using System.Linq;

namespace ApiPeliculas.Tests.Controllers
{
    public class CategoriasControllerTests
    {
        private readonly Mock<ICategoriaRepositorio> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CategoriasController>> _mockLogger;
        private readonly CategoriasController _controller;

        public CategoriasControllerTests()
        {
            _mockRepo = new Mock<ICategoriaRepositorio>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CategoriasController>>();
            _controller = new CategoriasController(_mockRepo.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public void GetCategorias_RetornaOkConListaDeCategorias()
        {
            // Arrange
            var categorias = new List<Categoria>
            {
                new Categoria { Id = 1, Nombre = "Acción", FechaCreacion = DateTime.Now },
                new Categoria { Id = 2, Nombre = "Comedia", FechaCreacion = DateTime.Now }
            };
            var categoriasDto = new List<CategoriaDto>
            {
                new CategoriaDto { Id = 1, Nombre = "Acción", FechaCreacion = DateTime.Now },
                new CategoriaDto { Id = 2, Nombre = "Comedia", FechaCreacion = DateTime.Now }
            };

            _mockRepo.Setup(r => r.GetCategorias()).Returns(categorias);
            _mockMapper.Setup(m => m.Map<CategoriaDto>(It.IsAny<Categoria>()))
                .Returns((Categoria c) => categoriasDto.First(d => d.Id == c.Id));

            // Act
            var result = _controller.GetCategorias();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<CategoriaDto>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
            Assert.Equal("Acción", returnValue[0].Nombre);
            Assert.Equal("Comedia", returnValue[1].Nombre);
        }

        [Fact]
        public void GetCategorias_CuandoListaVacia_RetornaOkConListaVacia()
        {
            // Arrange
            var categorias = new List<Categoria>();
            var categoriasDto = new List<CategoriaDto>();

            _mockRepo.Setup(r => r.GetCategorias()).Returns(categorias);
            _mockMapper.Setup(m => m.Map<CategoriaDto>(It.IsAny<Categoria>()))
                .Returns((Categoria c) => null);

            // Act
            var result = _controller.GetCategorias();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<CategoriaDto>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        [Fact]
        public void GetCategoria_Existente_RetornaOkConCategoria()
        {
            // Arrange
            var categoriaId = 1;
            var categoria = new Categoria { Id = categoriaId, Nombre = "Acción", FechaCreacion = DateTime.Now };
            var categoriaDto = new CategoriaDto { Id = categoriaId, Nombre = "Acción", FechaCreacion = DateTime.Now };

            _mockRepo.Setup(r => r.GetCategoria(categoriaId)).Returns(categoria);
            _mockMapper.Setup(m => m.Map<CategoriaDto>(categoria)).Returns(categoriaDto);

            // Act
            var result = _controller.GetCategoria(categoriaId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<CategoriaDto>(okResult.Value);
            Assert.Equal(categoriaId, returnValue.Id);
            Assert.Equal("Acción", returnValue.Nombre);
        }

        [Fact]
        public void GetCategoria_NoExistente_RetornaNotFound()
        {
            // Arrange
            var categoriaId = 999;
            _mockRepo.Setup(r => r.GetCategoria(categoriaId)).Returns((Categoria)null);

            // Act
            var result = _controller.GetCategoria(categoriaId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
