using Microsoft.Extensions.Caching.Memory;

namespace CompleteAuthenticationAPI.Middleware
{
    // OPCIÓN 1: Usando Middleware personalizado
    public class SessionTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionTimeoutMiddleware> _logger;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _timeoutDuration;

        public SessionTimeoutMiddleware(RequestDelegate next, ILogger<SessionTimeoutMiddleware> logger, IMemoryCache cache, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
            _timeoutDuration = TimeSpan.FromMinutes(Convert.ToInt32(configuration["SessionTimeOut"]!));
        }

        //Crea la sesión para el usuario autorizado
        public async Task InvokeAsync(HttpContext context)
        {
            // Verificar si es una ruta que requiere autenticación
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst("IdUsuario")?.Value;
                if (userId != null)
                {
                    string cacheKey = $"LastActivity_IdUser_{userId}";
                    DateTime? userLastActivity = _cache.Get<DateTime?>(cacheKey);
                    DateTime now = DateTime.Now;

                    if (userLastActivity.HasValue)
                    {
                        TimeSpan elapsedTime = now - userLastActivity.Value;
                        if (elapsedTime > _timeoutDuration)
                        {
                            string mensaje = $"Sesión expirada por inactividad para el usuario: '{userId}'. Favor volver a iniciar sesión.";
                            _logger.LogInformation(mensaje);

                            _cache.Remove(cacheKey);

                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync(mensaje);

                            return;
                        }
                    }

                    //Asigna una nueva sesión y a la vez borra automáticamente todas las sesiones que se encuentren inactivas
                    _cache.Set(cacheKey, now, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = _timeoutDuration.Add(TimeSpan.FromMinutes(1))
                    });
                }
            }

            await _next(context);
        }

    }
}
