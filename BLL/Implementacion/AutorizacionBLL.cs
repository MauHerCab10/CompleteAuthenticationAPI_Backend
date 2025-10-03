using Azure.Core;
using BLL.Interfaz;
using DAL.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Service.Interfaz;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Transversal.DTOs;
using Transversal.Model;

namespace Service.Implementacion
{
    public class AutorizacionBLL : IAutorizacionBLL
    {
        private readonly IConfiguration _configuration;
        private readonly IUtilidades _utilidades;
        private readonly IUsuarioBLL _usuarioBLL;
        private readonly IAutorizacionDAL _autorizacionDAL;

        public AutorizacionBLL(IConfiguration configuration, IUtilidades utilidades, IUsuarioBLL usuarioBLL, IAutorizacionDAL autorizacionDAL)
        {
            _configuration = configuration;
            _utilidades = utilidades;
            _usuarioBLL = usuarioBLL;
            _autorizacionDAL = autorizacionDAL;
        }


        #region Métodos Públicos

        //Genera el AccessToken y el RefreshToken, usando las credenciales de acceso del usuario
        public async Task<Respuesta<Usuario>> GenerarAccessTokenYRefreshTokenConCredenciales(LoginUsuarioDTO autorizacion)
        {
            var usuarioEncontrado = await _usuarioBLL.ConsultarUsuario(autorizacion.Email, autorizacion.Contrasena);
            if (usuarioEncontrado == null)
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "Usuario no encontrado. Favor validar los datos ingresados." };

            string accessTokenCreado = GenerarAccessToken(usuarioEncontrado.Objeto.IdUsuario.ToString());
            
            string refreshTokenCreado = GenerarRefreshToken();

            await EliminarHistorialRefreshTokenAnteriores(usuarioEncontrado.Objeto.IdUsuario);

            return await GuardarHistorialRefreshToken(usuarioEncontrado.Objeto.IdUsuario, accessTokenCreado, refreshTokenCreado);
        }


        //Genera tanto un AccessToken como un RefreshToken con base al RefreshToken del usuario
        public async Task<Respuesta<Usuario>> GenerarAccessTokenYRefreshTokenConRefreshTokenAnterior(int idUsuario, string accessToken, string refreshToken)
        {
            var refreshTokenEncontrado = await ConsultarUltimoHistorialRefreshTokensPorUsuario(idUsuario, accessToken, refreshToken);

            if (refreshTokenEncontrado == null)
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "El RefreshToken suministrado no existe o no se encuentra activo para ese usuario." };

            var accessTokenCreado = GenerarAccessToken(idUsuario.ToString());
            var refreshTokenCreado = GenerarRefreshToken();

            await EliminarHistorialRefreshTokenAnteriores(idUsuario);

            return await GuardarHistorialRefreshToken(idUsuario, accessTokenCreado, refreshTokenCreado);
        }


        //Consulta la FechaVencimiento del RefreshToken
        public async Task<DateTime?> ConsultarFechaVencimientoRefreshToken(int idUsuario)
        {
            var refreshTokenEncontrado = await ConsultarUltimoHistorialRefreshTokensPorUsuario(idUsuario);

            return refreshTokenEncontrado == null ? null : refreshTokenEncontrado.FechaExpiracion;
        }


        //Elimina todo el historial de Tokens del usuario encontrado
        public async Task<Respuesta<Usuario>> EliminarHistorialRefreshTokenAnteriores(int idUsuario)
        {
            var ultimoToken = await ConsultarUltimoHistorialRefreshTokensPorUsuario(idUsuario);

            if (ultimoToken == null)
            {
                return new Respuesta<Usuario>
                {
                    IsSuccess = false,
                    Mensaje = $"No existe ningún token activo del usuario '{idUsuario}' para eliminar."
                };
            }

            var esExitoso = await EliminarHistorialRefreshTokensPorUsuario(idUsuario);

            return new Respuesta<Usuario>
            {
                IsSuccess = esExitoso,
                Mensaje = $"Se eliminó todo el historial de tokens del usuario {idUsuario} generados anteriormente y que ya estaban vencidos."
            };
        }


        //Actualiza el AccessToken del usuario con base al RefreshToken encontrado
        public async Task<Respuesta<Usuario>> ActualizarAccessTokenConRefreshTokenAnterior(int idUsuario, string accessToken, string refreshToken)
        {
            var refreshTokenEncontrado = await ConsultarUltimoHistorialRefreshTokensPorUsuario(idUsuario, accessToken, refreshToken);

            if (refreshTokenEncontrado == null)
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "El AccessToken y/o el RefreshToken suministrados no existen, ó el RefreshToken no se encuentra activo para ese usuario." };

            var tokenCreado = GenerarAccessToken(idUsuario.ToString());

            return await ActualizaHistorialRefreshToken(accessToken, tokenCreado, refreshTokenEncontrado);
        }


        //Cierra la sesión del usuario borrando todos los token (activos e inactivos) del usuario
        public async Task<Respuesta<Usuario>> CerrarSesion(int idUsuario)
        {
            var tokensUsuario = await ConsultarUltimoHistorialRefreshTokensPorUsuario(idUsuario);

            if (tokensUsuario != null)
            {
                return new Respuesta<Usuario>
                {
                    IsSuccess = false,
                    Mensaje = $"No existen tokens activos del usuario '{idUsuario}' para eliminar."
                };
            }

            var esExitoso = await EliminarHistorialRefreshTokensPorUsuario(idUsuario);

            return new Respuesta<Usuario>
            {
                IsSuccess = esExitoso,
                Mensaje = $"Se eliminaron todos los Tokens del usuario '{idUsuario}'. ¡Sesión cerrada correctamente!"
            };
        }


        //Valida si el token ingresado es válido para realizar peticiones
        public bool ValidarToken(string token)
        {
            var claimsPrincipal = new ClaimsPrincipal();
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false, //valida q las apps externas puedan usar la URL donde se encuentra nuestra Api
                ValidateAudience = false, //quienes pueden acceder a nuestra Api
                ValidateLifetime = true, //valida el tiempo de vida del Token
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey
                (Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]!))
            };

            try
            {
                claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion Métodos Públicos



        #region Métodos Privados

        //Genera ÚNICAMENTE el AccesToken
        private string GenerarAccessToken(string idUsuario)
        {
            var key = _configuration.GetValue<string>("JwtSettings:SecretKey")!;
            var keyBytes = Encoding.ASCII.GetBytes(key);

            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, idUsuario));

            var credencialesToken = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature
            );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                NotBefore = _utilidades.FechaHoraActualColombia(),
                Expires = _utilidades.FechaHoraActualColombia().AddMinutes(_configuration.GetValue<int>("JwtSettings:AccessToken_ExpirationTime")), //AccessToken
                SigningCredentials = credencialesToken
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);
            string tokenCreado = tokenHandler.WriteToken(tokenConfig);

            return tokenCreado;
        }
        //public string GenerarJWT(Usuario modelo)
        //{
        //    var userClaims = new[]
        //    {
        //        new Claim(ClaimTypes.NameIdentifier, modelo.IdUsuario.ToString()),
        //        new Claim(ClaimTypes.Name, modelo.Email)
        //    };

        //    //creación de la Llave de Seguridad
        //    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]!));

        //    //creación de las Credenciales de seguridad
        //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

        //    //Parametrización del Token
        //    var configurationJWT = new JwtSecurityToken(
        //        claims: userClaims,
        //        expires: DateTime.UtcNow.AddMinutes(5), //usar una variable de appsettings
        //        signingCredentials: credentials
        //    );

        //    //Token generado
        //    var token = new JwtSecurityTokenHandler().WriteToken(configurationJWT);

        //    return token;
        //}

        //Genera ÚNICAMENTE el RefreshToken
        private string GenerarRefreshToken()
        {
            var byteArray = new byte[64];
            var refreshToken = "";

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(byteArray);
                refreshToken = Convert.ToBase64String(byteArray);
            }
            return refreshToken;
        }


        //Guarda el registro de historial del RefreshToken con el AccesToken
        private async Task<Respuesta<Usuario>> GuardarHistorialRefreshToken(int idUsuario, string accessToken, string refreshToken)
        {
            var historialRefreshToken = new HistorialRefreshToken
            {
                IdUsuario = idUsuario,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                FechaCreacion = _utilidades.FechaHoraActualColombia(),
                FechaExpiracion = _utilidades.FechaHoraActualColombia().AddMinutes(_configuration.GetValue<int>("JwtSettings:RefreshToken_ExpirationTime")) //RefreshToken
            };

            int idNuevoHistorialToken = await GuardarHistorialRefreshTokenDeUsuario(historialRefreshToken.IdUsuario, historialRefreshToken.AccessToken, historialRefreshToken.RefreshToken, historialRefreshToken.FechaCreacion, historialRefreshToken.FechaExpiracion);

            if (idNuevoHistorialToken > 0)
                return new Respuesta<Usuario> { IsSuccess = true, Mensaje = "¡AccessToken y RefreshToken generados OK!", Objeto = new Usuario { AccessToken = accessToken, RefreshToken = refreshToken } };
            else
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "¡Error al momento de generar el AccessToken y el RefreshToken!", Objeto = null! };
        }

        //Actualiza ÚNICAMENTE el AccessToken del RefreshToken encontrado
        private async Task<Respuesta<Usuario>> ActualizaHistorialRefreshToken(string anteriorAccessToken, string nuevoAccessToken, HistorialRefreshToken historialExistente)
        {
            var historialEncontrado = await ConsultarUltimoHistorialRefreshTokensPorUsuario(historialExistente.IdUsuario, anteriorAccessToken, historialExistente.RefreshToken);

            await ActualizarHistorialRefreshTokenDeUsuario(historialEncontrado.IdHistorialToken, nuevoAccessToken);

            return new Respuesta<Usuario> { IsSuccess = true, Mensaje = "¡AccessToken actualizado OK!", Objeto = new Usuario { AccessToken = nuevoAccessToken, RefreshToken = historialExistente.RefreshToken } };
        }

        // Consulta el último historial de Token que ha generado el usuario
        private async Task<HistorialRefreshToken> ConsultarUltimoHistorialRefreshTokensPorUsuario(int idUsuario, string? accessToken = null, string? refreshToken = null)
        {
            var ultimoAcceso = await _autorizacionDAL.ConsultarUltimoHistorialRefreshTokensPorUsuario(idUsuario, accessToken, refreshToken);
            return ultimoAcceso;
        }

        // Guarda el AccessToken y el RefreshToken del usuario
        private async Task<int> GuardarHistorialRefreshTokenDeUsuario(int idUsuario, string accessToken, string refreshToken, DateTime fechaCreacion, DateTime fechaExpiracion)
        {
            var idNuevoHistorialToken = await _autorizacionDAL.GuardarHistorialRefreshTokenDeUsuario(idUsuario, accessToken, refreshToken, fechaCreacion, fechaExpiracion);
            return idNuevoHistorialToken;
        }

        // Actualiza el AccessToken del usuario
        private async Task<bool> ActualizarHistorialRefreshTokenDeUsuario(int idHistorialToken, string accessToken)
        {
            var esExitoso = await _autorizacionDAL.ActualizarHistorialRefreshTokenDeUsuario(idHistorialToken, accessToken);
            return esExitoso;
        }

        // Elimina todo el historial completo de Tokens que ha generado el usuario a lo largo del tiempo
        private async Task<bool> EliminarHistorialRefreshTokensPorUsuario(int idUsuario)
        {
            var esExitoso = await _autorizacionDAL.EliminarHistorialRefreshTokensPorUsuario(idUsuario);
            return esExitoso;
        }

        #endregion Métodos Privados

    }
}
