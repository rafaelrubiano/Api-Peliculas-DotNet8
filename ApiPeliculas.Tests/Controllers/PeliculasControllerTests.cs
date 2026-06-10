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
    public class PeliculasControllerTests
    {
        private readonly Mock<IPeliculaRepositorio> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<PeliculasController>> _mockLogger;
        private readonly PeliculasController _controller;

        public PeliculasControllerTests()
        {
            _mockRepo = new Mock<IPeliculaRepositorio>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<PeliculasController>>();
            _controller = new PeliculasController(_mockRepo.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public void GetPeliculas_ConPaginacion_RetornaOkConMetadataDePaginacion()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 2;
            var totalPeliculas = 5;
            var peliculas = new List<Pelicula>
            {
                new Pelicula { Id = 1, Nombre = "Pelicula 1", categoriaId = 1 },
                new Pelicula { Id = 2, Nombre = "Pelicula 2", categoriaId = 1 }
            };
            var peliculasDto = new List<PeliculaDto>
            {
                new PeliculaDto { Id = 1, Nombre = "Pelicula 1" },
                new PeliculaDto { Id = 2, Nombre = "Pelicula 2" }
            };

            _mockRepo.Setup(r => r.GetTotalPeliculas()).Returns(totalPeliculas);
            _mockRepo.Setup(r => r.GetPeliculas(pageNumber, pageSize)).Returns(peliculas);
            _mockMapper.Setup(m => m.Map<PeliculaDto>(It.IsAny<Pelicula>()))
                .Returns((Pelicula p) => peliculasDto.First(d => d.Id == p.Id));

            // Act
            var result = _controller.GetPeliculas(pageNumber, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            var responseType = response.GetType();
            var pageNumberProp = responseType.GetProperty("pageNumber");
            var pageSizeProp = responseType.GetProperty("pageSize");
            var totalPagesProp = responseType.GetProperty("TotalPages");
            var totalItemsProp = responseType.GetProperty("TotalItems");
            var itemsProp = responseType.GetProperty("Items");

            Assert.NotNull(pageNumberProp);
            Assert.NotNull(pageSizeProp);
            Assert.NotNull(totalPagesProp);
            Assert.NotNull(totalItemsProp);
            Assert.NotNull(itemsProp);

            Assert.Equal(1, pageNumberProp.GetValue(response));
            Assert.Equal(2, pageSizeProp.GetValue(response));
            Assert.Equal(3, totalPagesProp.GetValue(response));
            Assert.Equal(5, totalItemsProp.GetValue(response));
            
            var items = itemsProp.GetValue(response) as List<PeliculaDto>;
            Assert.NotNull(items);
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public void GetPeliculas_SinResultados_RetornaNotFound()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 2;
            var peliculas = new List<Pelicula>();

            _mockRepo.Setup(r => r.GetTotalPeliculas()).Returns(0);
            _mockRepo.Setup(r => r.GetPeliculas(pageNumber, pageSize)).Returns(peliculas);

            // Act
            var result = _controller.GetPeliculas(pageNumber, pageSize);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No se encontraron películas.", notFoundResult.Value);
        }

        [Fact]
        public void GetPeliculas_ExcepcionEnRepositorio_Retorna500()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetTotalPeliculas())
                .Throws(new System.Exception("Error de base de datos"));

            // Act
            var result = _controller.GetPeliculas(1, 2);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Contains("Error recuperando datos", statusCodeResult.Value.ToString());
        }

        [Fact]
        public void GetPelicula_Existente_RetornaOkConPelicula()
        {
            // Arrange
            var peliculaId = 1;
            var pelicula = new Pelicula { Id = peliculaId, Nombre = "Inception", categoriaId = 1 };
            var peliculaDto = new PeliculaDto { Id = peliculaId, Nombre = "Inception" };

            _mockRepo.Setup(r => r.GetPelicula(peliculaId)).Returns(pelicula);
            _mockMapper.Setup(m => m.Map<PeliculaDto>(pelicula)).Returns(peliculaDto);

            // Act
            var result = _controller.GetPelicula(peliculaId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<PeliculaDto>(okResult.Value);
            Assert.Equal(peliculaId, returnValue.Id);
            Assert.Equal("Inception", returnValue.Nombre);
        }

        [Fact]
        public void GetPelicula_NoExistente_RetornaNotFound()
        {
            // Arrange
            var peliculaId = 999;
            _mockRepo.Setup(r => r.GetPelicula(peliculaId)).Returns((Pelicula)null);

            // Act
            var result = _controller.GetPelicula(peliculaId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
