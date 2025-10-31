using AutoMapper;
using Azure.Core;
using BLL.Interfaz;
using CompleteAuthenticationAPI.Middleware;
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

namespace CompleteAuthenticationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUsuarioBLL _usuario;
        private readonly IAutorizacionBLL _autorizacion;
        private readonly ICookieService _cookies;

        public UsuarioController(IConfiguration configuration, IUsuarioBLL usuarioBLL, IAutorizacionBLL autorizacionBLL, ICookieService cookies)
        {
            _configuration = configuration;
            _usuario = usuarioBLL;
            _autorizacion = autorizacionBLL;
            _cookies = cookies;
        }


        [HttpPost("RegistrarUsuario")] //1ro (Sign Up)
        public async Task<IActionResult> RegistrarUsuario([FromBody] UsuarioRegistroRequestDTO usuario)
        {
            var resultado = await _usuario.RegistrarUsuario(usuario);
            return Ok(new 
            {
                isSuccess = resultado.IsSuccess, 
                mensaje = resultado.Mensaje 
            });
        }

        [HttpGet("ConfirmarCuenta")] //2do (ejecutarlo mejor directamente desde el correo recibido)
        public async Task<IActionResult> ConfirmarCuenta(string guidAcceso)
        {
            Respuesta<UsuarioResponseDTO> resultado = await _usuario.ConfirmarCuenta(guidAcceso);
            return Redirect($"{_configuration.GetValue<string>("Frontend_URLs:Desarrollo")}/login?confirmacion={(resultado.IsSuccess ? "ok" : "error")}");
        }

        [HttpPost("AutenticarUsuario")] //3ro (Sign In)
        public async Task<IActionResult> AutenticarUsuario([FromBody] UsuarioLoginRequestDTO usuario)
        {
            var resultado = await _usuario.AutenticarUsuario(usuario);
            if (resultado.IsSuccess)
            {
                //_cookies.SetCookieAccessToken(resultado.Objeto.AccessToken);
                _cookies.SetCookieRefreshToken(resultado.Objeto.RefreshToken);

                return Ok(new 
                { 
                    isSuccess = resultado.IsSuccess, 
                    mensaje = resultado.Mensaje, 
                    idUsuario = resultado.Objeto.IdUsuario, 
                    accessToken = resultado.Objeto.AccessToken, 
                    /*refreshToken = resultado.Objeto.RefreshToken*/ 
                });
            }
            else
            {
                return Ok(new 
                { 
                    isSuccess = resultado.IsSuccess, 
                    mensaje = resultado.Mensaje 
                });
            }
        }

        [HttpPost("OlvidoSuContrasena")] //4to
        public async Task<IActionResult> OlvidoSuContrasena([FromBody] EmailDTO email)
        {
            var resultado = await _usuario.OlvidoSuContrasena(email.Email);
            return Ok(new
            {
                isSuccess = resultado.IsSuccess,
                mensaje = resultado.Mensaje
            });
        }

        [HttpPost("ActualizarContrasenaAntigua")] //5to
        public async Task<IActionResult> ActualizarContrasenaAntigua([FromBody] ActualizarContrasenaDTO contrasena)
        {
            var resultado = await _usuario.ActualizarContrasenaAntigua(contrasena.GuidAcceso, contrasena.NuevaContrasena, contrasena.ConfirmacionContrasena);
            return Ok(new 
            { 
                isSuccess = resultado.IsSuccess, 
                mensaje = resultado.Mensaje 
            });
        }

        //[Authorize]
        //[HttpPost("ObtenerRefreshToken")] //6to (no lo uso en el Frontend, pero usarlo solo en caso de extrema de necesidad)
        //public async Task<IActionResult> ObtenerRefreshToken()
        //{
        //    var idUsuario = HttpContext.Items["IdUsuario"]?.ToString();
        //    var accessToken = HttpContext.Items["AccessToken"]?.ToString();
        //    var refreshToken = HttpContext.Items["RefreshToken"]?.ToString();

        //    var resultado = await _autorizacion.GenerarAccessTokenYRefreshTokenConRefreshTokenAnterior(int.Parse(idUsuario!), accessToken!, refreshToken!);

        //    if (resultado.IsSuccess)
        //    {
        //        // Cargar las cookies en el navegador del usuario
        //        //_cookies.SetCookieAccessToken(resultado.Objeto.AccessToken);
        //        _cookies.SetCookieRefreshToken(resultado.Objeto.RefreshToken);

        //        return Ok(new 
        //        { 
        //            isSuccess = resultado.IsSuccess, 
        //            mensaje = resultado.Mensaje, 
        //            accessToken = resultado.Objeto.AccessToken 
        //        });
        //    }
        //    else
        //    {
        //        return BadRequest(resultado);
        //    }
        //}

        [Authorize]
        [HttpPost("CerrarSesion")] //6to
        public async Task<IActionResult> CerrarSesion()
        {
            var idUsuario = HttpContext.Items["IdUsuario"]?.ToString();

            var response = await _autorizacion.CerrarSesion(int.Parse(idUsuario!));

            if (response.IsSuccess)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [Authorize]
        [HttpGet("Ping")] //7mo (solo para PRUEBAS)
        public IActionResult Ping()
        {
            var idUsuario = HttpContext.Items["IdUsuario"]?.ToString();
            var accessToken = HttpContext.Items["AccessToken"]?.ToString();
            var refreshToken = HttpContext.Items["RefreshToken"]?.ToString();

            return Ok(new
            {
                message = "Pong",
                timestamp = DateTime.Now.ToString("dd/MMM/yyyy HH:mm:ss tt"),
                idUser = idUsuario,
                accessToken
            });
        }

    }
}
