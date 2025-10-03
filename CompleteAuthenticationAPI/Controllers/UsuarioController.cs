using AutoMapper;
using BLL.Interfaz;
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

        public UsuarioController(IUsuarioBLL usuarioBLL, IAutorizacionBLL autorizacionBLL, IConfiguration configuration)
        {
            _usuario = usuarioBLL;
            _autorizacion = autorizacionBLL;
            _configuration = configuration;
        }


        [HttpPost("RegistrarUsuario")] //1ro
        public async Task<IActionResult> RegistrarUsuario([FromBody] Usuario pUsuario) //me toca usar 'RegistroUsuarioDTO'
        {
            var resultado = await _usuario.RegistrarUsuario(pUsuario);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

        [HttpGet("ConfirmarCuenta")] //2do
        public async Task<IActionResult> ConfirmarCuenta(string guidAcceso)
        {
            bool resultado = await _usuario.ConfirmarCuenta(guidAcceso);
            return Ok(new { confirmacionCuenta = resultado });
        }

        [Authorize]
        [HttpGet("ValidarToken")] //3ro
        public IActionResult ValidarToken(string token) //cambiar esto para enviar el token como Bearer Token desde la pestaña de Authorization //preguntar a ChatGPT: Quiero que el token se valide automáticamente como JWT (usando AddAuthentication y [Authorize])
        {
            bool esTokenValido = _autorizacion.ValidarToken(token);
            return Ok(new { isSuccess = esTokenValido });
        }

        [HttpPost("AutenticarUsuario")] //4to
        public async Task<IActionResult> AutenticarUsuario([FromBody] Usuario pUsuario) //me toca usar 'LoginUsuarioDTO'
        {
            var resultado = await _usuario.AutenticarUsuario(pUsuario);
            if (resultado.IsSuccess)
            {
                //SetCookieAccessToken(resultado.Objeto.AccessToken);
                //SetCookieRefreshToken(resultado.Objeto.RefreshToken);
                return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje, idUsuario = resultado.Objeto.IdUsuario, accessToken = resultado.Objeto.AccessToken, refreshToken = resultado.Objeto.RefreshToken });
            }
            else
            {
                return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
            }
        }

        [HttpPost("RestablecerContrasena")] //5to
        public async Task<IActionResult> RestablecerContrasena(string email)
        {
            var resultado = await _usuario.ReestablecerContrasena(email);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

        [HttpPost("ActualizarContrasenaAntigua")] //6to
        public async Task<IActionResult> ActualizarContrasenaAntigua(string guidAcceso, string nuevaContrasena, string confirmacionContrasena)
        {
            var resultado = await _usuario.ActualizarContrasenaAntigua(guidAcceso, nuevaContrasena, confirmacionContrasena);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

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
                //SetCookieAccessToken(resultado.Objeto.AccessToken);
                //SetCookieRefreshToken(resultado.Objeto.RefreshToken);
                return Ok(resultado);
            }
            else
            {
                return BadRequest(resultado);
            }
        }

        [HttpPost("CerrarSesion")]
        [ServiceFilter(typeof(AdministradorHeaders))]
        public async Task<IActionResult> CerrarSesion()
        {
            var idUsuario = HttpContext.Items["IdUsuario"]?.ToString();

            var response = await _autorizacion.CerrarSesion(int.Parse(idUsuario!));

            //// Eliminar las cookies
            //Response.Cookies.Delete("accessToken");
            //Response.Cookies.Delete("refreshToken");

            if (response.IsSuccess)
                return Ok(response);
            else
                return BadRequest(response);
        }




        [Authorize]
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                message = "Pong",
                timestamp = DateTime.Now.ToString("dd/MMM/yyyy HH:mm:ss"),
                user = User.Identity?.Name
            });
        }






        //[Authorize] //probar si funciona con Authorize
        // Configurar Access Token en cookie HttpOnly
        private void SetCookieAccessToken(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,        // No accesible desde JavaScript
                Secure = true,          // Solo HTTPS (en producción)
                SameSite = SameSiteMode.Strict, // Protección CSRF
                Expires = DateTimeOffset.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:AccessToken_ExpirationTime")), //AccessToken
                Path = "/"              // Disponible en toda la app
            };

            Response.Cookies.Append("accessToken", token, cookieOptions);
        }

        //[Authorize] //probar si funciona con Authorize
        // Configurar Refresh Token en cookie HttpOnly
        private void SetCookieRefreshToken(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:RefreshToken_ExpirationTime")),  //RefreshToken
                Path = "/api/auth/refresh" // Solo accesible en endpoint de refresh
            };

            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }


    }
}
