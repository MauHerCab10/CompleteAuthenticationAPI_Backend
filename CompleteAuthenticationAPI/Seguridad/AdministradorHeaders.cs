using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Service.Implementacion;
using Service.Interfaz;
using System.IdentityModel.Tokens.Jwt;
using Transversal.Model;

namespace CompleteAuthenticationAPI.Seguridad
{
    public class AdministradorHeaders : Attribute, IAuthorizationFilter
    {
        private readonly IConfiguration _configuration;
        private readonly IAutorizacionBLL _autorizacion;
        private readonly IUtilidades _utilidades;

        public AdministradorHeaders(IConfiguration configuration, IAutorizacionBLL autorizacion, IUtilidades utilidades)
        {
            _configuration = configuration;
            _autorizacion = autorizacion;
            _utilidades = utilidades;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            try
            {
                var headers = context.HttpContext.Request.Headers;

                var accessToken = headers["AccessToken"].FirstOrDefault();
                var refreshToken = headers["RefreshToken"].FirstOrDefault();

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    context.Result = new BadRequestObjectResult(new Respuesta<Usuario>
                    {
                        IsSuccess = false,
                        Mensaje = "Favor validar que tanto el AccessToken como el RefreshToken sean enviados."
                    });
                    return;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(accessToken);
                string idUsuario = jwt.Claims.First(x => x.Type == JwtRegisteredClaimNames.NameId).Value;

                DateTime realDatetimeTokenExpiradoSupuestamente = jwt.ValidTo.AddHours(_configuration.GetValue<int>("JwtSettings:CantidadHorasRestarZonaHoraria"));
                DateTime datetimeActualColombia = _utilidades.FechaHoraActualColombia();
                DateTime? fechaVencimientoRefreshToken = _autorizacion.ConsultarFechaVencimientoRefreshToken(int.Parse(idUsuario)).Result;

                if (fechaVencimientoRefreshToken is null)
                {
                    context.Result = new BadRequestObjectResult(new Respuesta<Usuario>
                    {
                        IsSuccess = false,
                        Mensaje = $"No existe ningún token activo para el usuario '{idUsuario}'. Favor iniciar sesión nuevamente."
                    });
                    return;
                }

                if (fechaVencimientoRefreshToken < datetimeActualColombia)
                {
                    context.Result = new BadRequestObjectResult(new Respuesta<Usuario>
                    {
                        IsSuccess = false,
                        Mensaje = "RefreshToken ya ha expirado. Favor ingresar nuevamente con sus credenciales de acceso."
                    });
                    return;
                }

                Task<Respuesta<Usuario>> autorizacion = _autorizacion.ActualizarAccessTokenConRefreshTokenAnterior(int.Parse(idUsuario), accessToken, refreshToken);

                if (!autorizacion.Result.IsSuccess)
                {
                    context.Result = new BadRequestObjectResult(new Respuesta<Usuario>
                    {
                        IsSuccess = false,
                        Mensaje = autorizacion.Result.Mensaje
                    });
                    return;
                }

                context.HttpContext.Items["IdUsuario"] = idUsuario;
                context.HttpContext.Items["AccessToken"] = autorizacion.Result.Objeto.AccessToken;
                context.HttpContext.Items["RefreshToken"] = refreshToken;
            }
            catch (Exception ex)
            {
                context.Result = new ObjectResult(new Respuesta<Usuario>
                {
                    IsSuccess = false,
                    Mensaje = $"Ocurrió un error en la autorización. AccessToken inválido. {ex}"
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

    }
}
