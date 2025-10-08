using CompleteAuthenticationAPI.Cookies;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICookieService _cookies;
        private readonly IConfiguration _configuration;
        private readonly IAutorizacionBLL _autorizacion;
        private readonly IUtilidades _utilidades;

        public AdministradorHeaders(IHttpContextAccessor httpContextAccessor, ICookieService cookies, IConfiguration configuration, IAutorizacionBLL autorizacion, IUtilidades utilidades)
        {
            _httpContextAccessor = httpContextAccessor;
            _cookies = cookies;
            _configuration = configuration;
            _autorizacion = autorizacion;
            _utilidades = utilidades;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            try
            {
                //var headers = context.HttpContext.Request.Headers;
                //var accessToken = headers["AccessToken"].FirstOrDefault();
                //var refreshToken = headers["RefreshToken"].FirstOrDefault();

                var cookies = _httpContextAccessor.HttpContext;
                cookies!.Request.Cookies.TryGetValue("cookieAccessToken", out string? accessToken);
                cookies!.Request.Cookies.TryGetValue("cookieRefreshToken", out string? refreshToken);

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

                //Cargar las cookies en el navegador del usuario
                //context.HttpContext.Items["AccessToken"] = autorizacion.Result.Objeto.AccessToken;
                //context.HttpContext.Items["RefreshToken"] = refreshToken;
                _cookies.SetCookieAccessToken(autorizacion.Result.Objeto.AccessToken);
                _cookies.SetCookieRefreshToken(refreshToken);

                context.HttpContext.Items["IdUsuario"] = idUsuario;
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
