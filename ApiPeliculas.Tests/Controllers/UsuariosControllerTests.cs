using Xunit;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiPeliculas.Controllers;
using ApiPeliculas.Modelos;
using ApiPeliculas.Modelos.Dtos;
using ApiPeliculas.Repositorio.IRepositorio;
using System.Net;
using System.Threading.Tasks;

namespace ApiPeliculas.Tests.Controllers
{
    public class UsuariosControllerTests
    {
        private readonly Mock<IUsuarioRepositorio> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UsuariosController>> _mockLogger;
        private readonly UsuariosController _controller;

        public UsuariosControllerTests()
        {
            _mockRepo = new Mock<IUsuarioRepositorio>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UsuariosController>>();
            _controller = new UsuariosController(_mockRepo.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Registro_UsuarioUnico_RetornaCreatedConUsuario()
        {
            // Arrange
            var registroDto = new UsuarioRegistroDto
            {
                NombreUsuario = "nuevo_usuario",
                Nombre = "Nuevo",
                Password = "password123",
                Role = "User"
            };

            var usuarioCreado = new UsuarioDatosDto
            {
                ID = "1",
                Username = "nuevo_usuario",
                Nombre = "Nuevo"
            };

            _mockRepo.Setup(r => r.IsUniqueUser(registroDto.NombreUsuario)).Returns(true);
            _mockRepo.Setup(r => r.Registro(registroDto)).ReturnsAsync(usuarioCreado);

            // Act
            var result = await _controller.Registro(registroDto);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var respuesta = Assert.IsType<RespuestaAPI>(createdResult.Value);
            
            Assert.True(respuesta.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Empty(respuesta.ErrorMessages);
            Assert.NotNull(respuesta.Result);
            
            var usuarioRespuesta = Assert.IsType<UsuarioDatosDto>(respuesta.Result);
            Assert.Equal("nuevo_usuario", usuarioRespuesta.Username);
        }

        [Fact]
        public async Task Registro_UsuarioDuplicado_RetornaBadRequest()
        {
            // Arrange
            var registroDto = new UsuarioRegistroDto
            {
                NombreUsuario = "usuario_existente",
                Nombre = "Existente",
                Password = "password123",
                Role = "User"
            };

            _mockRepo.Setup(r => r.IsUniqueUser(registroDto.NombreUsuario)).Returns(false);

            // Act
            var result = await _controller.Registro(registroDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var respuesta = Assert.IsType<RespuestaAPI>(badRequestResult.Value);
            
            Assert.False(respuesta.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Single(respuesta.ErrorMessages);
            Assert.Contains("nombre de usuario ya existe", respuesta.ErrorMessages[0]);
            Assert.Null(respuesta.Result);
            
            _mockRepo.Verify(r => r.Registro(It.IsAny<UsuarioRegistroDto>()), Times.Never);
        }

        [Fact]
        public async Task Registro_RegistroFallido_RetornaBadRequest()
        {
            // Arrange
            var registroDto = new UsuarioRegistroDto
            {
                NombreUsuario = "nuevo_usuario",
                Nombre = "Nuevo",
                Password = "password123",
                Role = "User"
            };

            _mockRepo.Setup(r => r.IsUniqueUser(registroDto.NombreUsuario)).Returns(true);
            _mockRepo.Setup(r => r.Registro(registroDto)).ReturnsAsync((UsuarioDatosDto)null);

            // Act
            var result = await _controller.Registro(registroDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var respuesta = Assert.IsType<RespuestaAPI>(badRequestResult.Value);
            
            Assert.False(respuesta.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Contains("Error en el registro", respuesta.ErrorMessages[0]);
        }

        [Fact]
        public async Task Registro_ExcepcionEnRegistro_Retorna500()
        {
            // Arrange
            var registroDto = new UsuarioRegistroDto
            {
                NombreUsuario = "nuevo_usuario",
                Nombre = "Nuevo",
                Password = "password123",
                Role = "User"
            };

            _mockRepo.Setup(r => r.IsUniqueUser(registroDto.NombreUsuario)).Returns(true);
            _mockRepo.Setup(r => r.Registro(registroDto))
                .ThrowsAsync(new System.Exception("Error de conexión a base de datos"));

            // Act
            var result = await _controller.Registro(registroDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var respuesta = Assert.IsType<RespuestaAPI>(statusCodeResult.Value);
            
            Assert.False(respuesta.IsSuccess);
            Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
            Assert.Contains("Error de conexión", respuesta.ErrorMessages[0]);
        }
    }
}
