using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Service.Interfaz;

namespace Service.Implementacion
{
    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public CookieService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        //Creación de la cookie de AccessToken
        public void SetCookieAccessToken(string token)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                throw new InvalidOperationException("No HttpContext available [SetCookieAccessToken].");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:AccessToken_ExpirationTime")), //AccessToken
                Path = "/"
            };

            context.Response.Cookies.Append("cookieAccessToken", token, cookieOptions);
        }

        //Creación de la cookie de RefreshToken
        public void SetCookieRefreshToken(string token)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                throw new InvalidOperationException("No HttpContext available [SetCookieRefreshToken].");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:RefreshToken_ExpirationTime")), //RefreshToken
                Path = "/"
            };

            context.Response.Cookies.Append("cookieRefreshToken", token, cookieOptions);
        }

        //Eliminación de las cookies en el navegador del usuario cuando la respuesta llegue al frontend
        public void EliminarCookiesDelUsuario()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                throw new InvalidOperationException("No HttpContext available [EliminarCookiesDelUsuario].");

            context.Response.Cookies.Delete("cookieAccessToken");
            context.Response.Cookies.Delete("cookieRefreshToken");
        }

    }
}
