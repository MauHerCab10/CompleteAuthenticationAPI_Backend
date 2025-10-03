using AutoMapper;
using BLL.Interfaz;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IUtilidades _utilidades;
        private readonly IUsuarioBLL _usuarioBLL;
        private readonly IAutorizacionBLL _autorizacionBLL;

        public UsuarioController(IUtilidades utilidades, IUsuarioBLL usuarioBLL, IAutorizacionBLL autorizacionBLL)
        {
            _utilidades = utilidades;
            _usuarioBLL = usuarioBLL;
            _autorizacionBLL = autorizacionBLL;
        }


        [HttpPost("RegistrarUsuario")] //1ro
        public async Task<IActionResult> RegistrarUsuario([FromBody] Usuario pUsuario) //me toca usar 'RegistroUsuarioDTO'
        {
            var resultado = await _usuarioBLL.RegistrarUsuario(pUsuario);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

        [HttpGet("ConfirmarCuenta")] //2do
        public async Task<IActionResult> ConfirmarCuenta(string guidAcceso)
        {
            bool resultado = await _usuarioBLL.ConfirmarCuenta(guidAcceso);
            return Ok(new { confirmacionCuenta = resultado });
        }

        [Authorize]
        [HttpGet("ValidarToken")] //3ro
        public IActionResult ValidarToken(string token) //cambiar esto para enviar el token como Bearer Token desde la pestaña de Authorization //preguntar a ChatGPT: Quiero que el token se valide automáticamente como JWT (usando AddAuthentication y [Authorize])
        {
            bool esTokenValido = _autorizacionBLL.ValidarToken(token);
            return Ok(new { isSuccess = esTokenValido });
        }

        [HttpPost("AutenticarUsuario")] //4to
        public async Task<IActionResult> AutenticarUsuario([FromBody] Usuario pUsuario) //me toca usar 'LoginUsuarioDTO'
        {
            var resultado = await _usuarioBLL.AutenticarUsuario(pUsuario);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje, idUsuario = resultado.Objeto.IdUsuario, token = resultado.Objeto.AccessToken });
        }

        [HttpPost("RestablecerContrasena")] //5to
        public async Task<IActionResult> RestablecerContrasena(string email)
        {
            var resultado = await _usuarioBLL.ReestablecerContrasena(email);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
        }

        [HttpPost("ActualizarContrasenaAntigua")] //6to
        public async Task<IActionResult> ActualizarContrasenaAntigua(string guidAcceso, string nuevaContrasena, string confirmacionContrasena)
        {
            var resultado = await _usuarioBLL.ActualizarContrasenaAntigua(guidAcceso, nuevaContrasena, confirmacionContrasena);
            return Ok(new { isSuccess = resultado.IsSuccess, mensaje = resultado.Mensaje });
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

    }
}
