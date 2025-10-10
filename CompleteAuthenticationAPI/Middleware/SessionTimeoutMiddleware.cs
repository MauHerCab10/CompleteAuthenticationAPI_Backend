namespace CompleteAuthenticationAPI.Middleware
{
    // OPCIÓN 1: Usando Middleware personalizado
    public class SessionTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionTimeoutMiddleware> _logger;
        private readonly TimeSpan _timeoutDuration;

        public SessionTimeoutMiddleware(RequestDelegate next, ILogger<SessionTimeoutMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _timeoutDuration = TimeSpan.FromMinutes(Convert.ToInt32(configuration["SessionTimeOut"]!));
        }

        //Crea la sesión para el usuario autorizado
        public async Task InvokeAsync(HttpContext context)
        {
            // Verificar si es una ruta que requiere autenticación
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userLastActivity = context.Session.GetString($"LastActivity_IdUser_{context.User.Identity.Name}");
                var now = DateTime.Now;

                if (!string.IsNullOrEmpty(userLastActivity))
                {
                    var lastActivityTime = DateTime.Parse(userLastActivity);

                    TimeSpan elapsedTime = now - lastActivityTime;
                    if (elapsedTime > _timeoutDuration)
                    {
                        string mensaje = $"Sesión expirada por inactividad para el usuario: '{context.User.Identity.Name}'.";
                        _logger.LogInformation(mensaje);

                        context.Session.Clear();
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync(mensaje);
                        return;
                    }
                }

                // Actualizar último tiempo de actividad
                context.Session.SetString($"LastActivity_IdUser_{context.User.Identity.Name}", now.ToString("O"));
            }

            await _next(context);
        }

    }
}
