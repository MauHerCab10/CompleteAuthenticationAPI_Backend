namespace CompleteAuthenticationAPI.Cookies
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

        // Configuración de AccessToken en cookie HttpOnly
        public void SetCookieAccessToken(string token)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                throw new InvalidOperationException("No HttpContext available.");

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

        // Configuración de RefreshToken en cookie HttpOnly
        public void SetCookieRefreshToken(string token)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                throw new InvalidOperationException("No HttpContext available.");

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

    }
}
