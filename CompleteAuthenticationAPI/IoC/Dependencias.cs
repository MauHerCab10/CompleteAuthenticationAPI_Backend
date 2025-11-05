using BLL.Implementacion;
using BLL.Interfaz;
using CompleteAuthenticationAPI.Middleware;
using DAL.Implementacion;
using DAL.Interfaz;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Service.Implementacion;
using Service.Interfaz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

namespace Transversal.Helper
{
    public static class Dependencias
    {
        //Dentro del servicio "IServiceCollection" q es creado automaticamente por la aplicación, se le va a agregar los métodos de "InyectarDependencias()" y "ConfigurarInicializacionAplicacionWeb()"
        //A este concepto se le llama "método de extensión", pq se le agrega un método nuevo a una clase q existe por defecto cuando se creó la aplicación

        public static void InyectarDependencias(this IServiceCollection services, IConfiguration configuration)
        {
            // Add services to the container.

            services.AddControllers();
            //services.AddAuthentication();
            services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            //Guardar en memoria Caché todas las caché de la aplicación (plantillas de los correos y fechas con hora de la última actividad por cada usuario q realice una petición)
            services.AddMemoryCache();

            //Capturar el contexto HTTP del servidor para ser usado dentro de la clase de una biblioteca de clases
            services.AddHttpContextAccessor();

            //Implementación del Automapper (de los modelos de BD a los DTO y viceversa)
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());

            //// Configurar sesiones
            //services.AddDistributedMemoryCache();
            //services.AddSession(options =>
            //{
            //    // Se le da a la sesión del servidor un tiempo de vida MAYOR q el q tiene "SessionTimeOut", esto evita que el servidor borre la sesión antes de que 'SessionTimeoutMiddleware' la verifique, esto para evitar q se pisen los tiempos
            //    options.IdleTimeout = TimeSpan.FromMinutes(Convert.ToInt32(configuration["SessionTimeOut"]!) + 1);
            //    options.Cookie.HttpOnly = true;
            //    options.Cookie.IsEssential = true;
            //});

            //Inyección de Dependencias
            services.AddSingleton<IUtilidades, Utilidades>();
            services.AddScoped<ICookieService, CookieService>();
            services.AddScoped<IUsuarioDAL, UsuarioDAL>();
            services.AddScoped<IUsuarioBLL, UsuarioBLL>();
            services.AddScoped<IAutorizacionDAL, AutorizacionDAL>();
            services.AddScoped<IAutorizacionBLL, AutorizacionBLL>();
            services.AddScoped<IPlantillaCorreoDAL, PlantillaCorreoDAL>();
            services.AddScoped<IPlantillasCorreoService, PlantillasCorreoService>();
            services.Configure<ServidorEmail>(configuration.GetSection("ServidorEmail"));

            //JSON Web Token (JWT) configuration
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(jwtConfig =>
            {
                jwtConfig.RequireHttpsMetadata = false;
                jwtConfig.SaveToken = true;

                //configuración y parametrización del AccessToken
                jwtConfig.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true, //verifica la firma del token usando la clave secreta (SecretKey). Esto garantiza que nadie haya modificado el token
                    ValidateIssuer = true, //comprueba que el token proviene del emisor correcto ("fullauth-api.com")
                    ValidIssuer = configuration["JwtSettings:Issuer"], //valor esperado del emisor (JwtSettings:Issuer)
                    ValidateAudience = true, //asegura que el token esté destinado a esta API
                    ValidAudience = configuration["JwtSettings:Audience"], //valor esperado de la audiencia (JwtSettings:Audience)
                    ValidateLifetime = false, //controla si el tiempo de vida del Token será verificado durante la validación (lo valido manualmente en AdministradorHeadersMiddleware)
                    ClockSkew = TimeSpan.Zero, //elimina la tolerancia por desfase de reloj
                    NameClaimType = ClaimTypes.NameIdentifier, //indican qué claim se usará como nombre del usuario
                    RoleClaimType = ClaimTypes.Role, //indican qué claim se usará como rol del usuario
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]!) //la clave secreta que se usa para validar la firma del token. Si no coincide, el token es inválido
                    )
                };

                //configuración para obtener el AccessToken de las Cookies
                jwtConfig.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        //Primero intenta leer la Cookie del header Authorization
                        var token = context.Request.Headers["Authorization"]
                            .FirstOrDefault()?
                            .Split(" ")
                            .Last();

                        ////Si no está en el header, busca como tal en la Cookie
                        //if (string.IsNullOrEmpty(token))
                        //    token = context.Request.Cookies["cookieAccessToken"];
                        
                        //Si encontró el valor del AccessToken, entonces lo asigna y lo retorna
                        if (!string.IsNullOrEmpty(token))
                            context.Token = token;
                        
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Autenticación fallida: {context.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };
            });

            //Habilitar CORS
            services.AddCors(options =>
            {
                options.AddPolicy("PolicyCORS", app =>
                {
                    app
                    .WithOrigins(
                        configuration["Frontend_URLs:Desarrollo"]!,
                        configuration["Frontend_URLs:Produccion"]!
                     )
                    .AllowCredentials() //permite cargar las cookies en el navegador
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

        }

        public static void ConfigurarInicializacionAplicacionWeb(this IServiceCollection services, WebApplication app)
        {
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("PolicyCORS");

            //Para control y manejo de Cookies
            app.UseHttpsRedirection();
            app.UseHsts();

            //app.UseSession();

            //Middlewares (el orden de ejecución va de arriba para abajo)
            app.UseMiddleware<AdministradorHeadersMiddleware>();
            //app.UseMiddleware<SessionTimeoutMiddleware>(); //se apaga ya q en el Frontend se valida la actividad del usuario (este middleware solo tiene en cuenta las peticiones q lleguen al Backend)

            //app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

    }
}
