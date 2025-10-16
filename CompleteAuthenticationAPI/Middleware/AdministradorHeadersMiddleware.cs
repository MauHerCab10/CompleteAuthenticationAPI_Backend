using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Service.Implementacion;
using Service.Interfaz;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Transversal.Model;

namespace CompleteAuthenticationAPI.Middleware
{
    public class AdministradorHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdministradorHeadersMiddleware> _logger;

        public AdministradorHeadersMiddleware(RequestDelegate next, ILogger<AdministradorHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICookieService cookies, IConfiguration configuration, IAutorizacionBLL autorizacion, IUtilidades utilidades)
        {
            try
            {
                //Verificar si el endpoint requiere autorización
                var endpoint = context.GetEndpoint();

                //Si el endpoint no existe, o no tiene [Authorize] o tiene [AllowAnonymous], entonces saltamos este middleware
                if (endpoint == null || endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null || endpoint.Metadata.GetMetadata<AuthorizeAttribute>() == null)
                {
                    await _next(context);
                    return;
                }

                //Headers
                string accessToken = context.Request.Headers["Authorization"].FirstOrDefault()!.Replace("Bearer ", string.Empty); //AccessToken

                //Cookies
                context!.Request.Cookies.TryGetValue("cookieRefreshToken", out string? refreshToken); //RefreshToken

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        IsSuccess = false,
                        Mensaje = "Favor validar que tanto el AccessToken como el RefreshToken sean enviados."
                    });
                    return;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(accessToken);
                string idUsuario = jwt.Claims.First(x => x.Type == "IdUsuario").Value;

                DateTime datetimeActualColombia = utilidades.FechaHoraActualColombia();
                DateTime fechaExpiracionAccessToken = jwt.ValidTo.AddHours(configuration.GetValue<int>("JwtSettings:CantidadHorasRestarZonaHoraria"));
                DateTime? fechaExpiracionRefreshToken = autorizacion.ConsultarFechaVencimientoRefreshToken(int.Parse(idUsuario)).Result;

                if (fechaExpiracionRefreshToken is null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new Respuesta<Usuario>
                    {
                        IsSuccess = false,
                        Mensaje = $"No existe ningún token activo para el usuario '{idUsuario}'. Favor iniciar sesión nuevamente."
                    });
                    return;
                }

                if (fechaExpiracionRefreshToken < datetimeActualColombia)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new Respuesta<Usuario>
                    {
                        IsSuccess = false,
                        Mensaje = "RefreshToken ya ha expirado. Favor ingresar nuevamente con sus credenciales de acceso."
                    });
                    return;
                }

                //Si el AccessToken ya está vencido, pero el RefreshToken sigue aun vigente, se procede a crear un nuevo AccessToken
                Task<Respuesta<Usuario>> respuesta = Task.Run(() => new Respuesta<Usuario>());
                if (fechaExpiracionAccessToken < datetimeActualColombia && fechaExpiracionRefreshToken > datetimeActualColombia)
                {
                    //Por cada petición que requiera autenticación (AccessToken de por medio), se actualiza el AccessToken para mantenerle refrescando su tiempo de expiración en el servidor debido a su corto tiempo de vida
                    respuesta = autorizacion.ActualizarAccessTokenConRefreshTokenAnterior(int.Parse(idUsuario), accessToken, refreshToken);

                    if (!respuesta.Result.IsSuccess)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsJsonAsync(new Respuesta<Usuario>
                        {
                            IsSuccess = false,
                            Mensaje = respuesta.Result.Mensaje
                        });
                        return;
                    }
                }
                
                string newAccessToken 
                    = respuesta.Result.Objeto is null 
                    ? accessToken 
                    : respuesta.Result.Objeto.AccessToken;

                //Cargar las cookies en el navegador del usuario (quedan actualizadas para la siguiente petición entrante)
                //_cookies.SetCookieAccessToken(autorizacion.Result.Objeto.AccessToken);
                cookies.SetCookieRefreshToken(refreshToken);

                //Variables globales a nivel local del servidor (nunca se envían al Frontend)
                context.Items["IdUsuario"] = idUsuario;
                context.Items["AccessToken"] = newAccessToken;
                context.Items["RefreshToken"] = refreshToken;

                ActualizarContextoHttpDelUsuario(context, newAccessToken);

                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    IsSuccess = false,
                    Mensaje = $"Ocurrió un error en la autorización. AccessToken inválido. {ex}"
                });
                return;
            }
        }


        private void ActualizarContextoHttpDelUsuario(HttpContext context, string newAccessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(newAccessToken);

            var claims = jwtToken.Claims;
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            context.User = principal;
        }

    }
}
