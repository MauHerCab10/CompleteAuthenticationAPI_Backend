using AutoMapper;
using BLL.Interfaz;
using CompleteAuthenticationAPI.Cookies;
using CompleteAuthenticationAPI.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Service.Implementacion;
using Service.Interfaz;
using System.Reflection;
using System.Security.Claims;
using Transversal.DTOs;
using Transversal.Model;
using Transversal.Service;

namespace CompleteAuthenticationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioBLL _usuario;
        private readonly IAutorizacionBLL _autorizacion;
        private readonly IConfiguration _configuration;
        private readonly ICookieService _cookies;

        public UsuarioController(IUsuarioBLL usuarioBLL, IAutorizacionBLL autorizacionBLL, IConfiguration configuration, ICookieService cookies)
        {
            _usuario = usuarioBLL;
            _autorizacion = autorizacionBLL;
            _configuration = configuration;
            _cookies = cookies;
        }


        [HttpPost("RegistrarUsuario")] //1ro
        public async Task<IActionResult> RegistrarUsuario([FromBody] Usuario pUsuario) //me toca usar 'RegistroUsuarioDTO'
        {
            var resultado = await _usuario.RegistrarUsuario(pUsuario);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

        [HttpGet("ConfirmarCuenta")] //2do (ejecutarlo mejor directamente desde el correo)
        public async Task<IActionResult> ConfirmarCuenta(string guidAcceso)
        {
            Respuesta<Usuario> resultado = await _usuario.ConfirmarCuenta(guidAcceso);
            return Ok(new { confirmacionCuenta = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

        [HttpPost("AutenticarUsuario")] //3ro
        public async Task<IActionResult> AutenticarUsuario([FromBody] Usuario pUsuario) //me toca usar 'LoginUsuarioDTO'
        {
            var resultado = await _usuario.AutenticarUsuario(pUsuario);
            if (resultado.IsSuccess)
            {
                _cookies.SetCookieAccessToken(resultado.Objeto.AccessToken);
                _cookies.SetCookieRefreshToken(resultado.Objeto.RefreshToken);
                return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje, idUsuario = resultado.Objeto.IdUsuario, accessToken = resultado.Objeto.AccessToken, refreshToken = resultado.Objeto.RefreshToken });
            }
            else
            {
                return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
            }
        }

        [HttpPost("OlvidoSuContrasena")] //4to
        public async Task<IActionResult> OlvidoSuContrasena([FromBody] string email)
        {
            var resultado = await _usuario.OlvidoSuContrasena(email);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

        [HttpGet("RestablecerContrasena")] //5to (ejecutarlo mejor directamente desde el correo)
        public async Task<IActionResult> RestablecerContrasena(string guidAcceso)
        {
            var resultado = await _usuario.ConsultarUsuarioPorGuid(guidAcceso);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

        [HttpPost("ActualizarContrasenaAntigua")] //6to (se puede desde Postman, pero ejecutarlo mejor desde la pantalla de RestablecerContraseña)
        public async Task<IActionResult> ActualizarContrasenaAntigua([FromBody] ActualizarContrasenaDTO contrasena)
        {
            var resultado = await _usuario.ActualizarContrasenaAntigua(contrasena.GuidAcceso, contrasena.NuevaContrasena, contrasena.ConfirmacionContrasena);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

        [Authorize]
        [HttpGet("ValidarToken")] //7mo
        [ServiceFilter(typeof(AdministradorHeaders))]
        public IActionResult ValidarToken()
        {
            bool esTokenValido = false;

            if (Request.Cookies.TryGetValue("cookieAccessToken", out string? token))
                esTokenValido = _autorizacion.ValidarToken(token);

            return Ok(new { isSuccess = esTokenValido });
        }

        [Authorize]
        [HttpPost("ObtenerRefreshToken")]
        [ServiceFilter(typeof(AdministradorHeaders))]
        public async Task<IActionResult> ObtenerRefreshToken() //[FromBody] RefreshTokenRequest request
        {
            var idUsuario = HttpContext.Items["IdUsuario"]?.ToString();
            var accessToken = HttpContext.Items["AccessToken"]?.ToString();
            var refreshToken = HttpContext.Items["RefreshToken"]?.ToString();

            var resultado = await _autorizacion.GenerarAccessTokenYRefreshTokenConRefreshTokenAnterior(int.Parse(idUsuario!), accessToken!, refreshToken!);

            if (resultado.IsSuccess)
            {
                // Cargar las cookies en el navegador del usuario
                _cookies.SetCookieAccessToken(resultado.Objeto.AccessToken);
                _cookies.SetCookieRefreshToken(resultado.Objeto.RefreshToken);
                return Ok(resultado);
            }
            else
            {
                return BadRequest(resultado);
            }
        }

        [Authorize]
        [HttpPost("CerrarSesion")]
        [ServiceFilter(typeof(AdministradorHeaders))]
        public async Task<IActionResult> CerrarSesion()
        {
            var idUsuario = HttpContext.Items["IdUsuario"]?.ToString();

            var response = await _autorizacion.CerrarSesion(int.Parse(idUsuario!));

            // Eliminar las cookies del navegador del usuario
            _cookies.EliminarCookiesDelUsuario();

            if (response.IsSuccess)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [Authorize]
        [HttpGet("Ping")]
        [ServiceFilter(typeof(AdministradorHeaders))]
        public IActionResult Ping()
        {
            return Ok(new
            {
                message = "Pong",
                timestamp = DateTime.Now.ToString("dd/MMM/yyyy HH:mm:ss"),
                user = User.Identity?.Name
            });
        }

    }
}
