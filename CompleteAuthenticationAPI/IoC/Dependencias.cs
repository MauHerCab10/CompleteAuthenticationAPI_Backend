using BLL.Implementacion;
using BLL.Interfaz;
using CompleteAuthenticationAPI.Cookies;
using CompleteAuthenticationAPI.Middleware;
using CompleteAuthenticationAPI.Seguridad;
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
using Transversal.Service;

namespace Transversal.Helper
{
    public static class Dependencias
    {
        //Dentro del servicio "IServiceCollection" q es creado automaticamente por la aplicación, se le va a agregar este método de "InyectarDependencias"
        //A este concepto se le llama "método de extensión", pq se le agrega un método nuevo a una clase ya existente
        public static void InyectarDependencias(this IServiceCollection services, IConfiguration configuration)
        {
            // Add services to the container.

            services.AddControllers();
            services.AddAuthentication();
            services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            //Guardar en memoria Caché las Plantillas de los correos
            services.AddMemoryCache();

            //Capturar la URL del servidor dentro del método de la clase de una biblioteca de clases
            services.AddHttpContextAccessor();

            //Implementación del Automapper (de los modelos de BD a los DTO y viceversa)
            services.AddAutoMapper(typeof(AutoMapperProfile));

            // Configurar sesiones
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(Convert.ToInt32(configuration["SessionTimeOut"]!));
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            //Inyección de Dependencias
            services.AddScoped<AdministradorHeaders>();
            services.AddSingleton<IUtilidades, Utilidades>();
            services.AddScoped<ICookieService, CookieService>();
            services.AddScoped<IUsuarioDAL, UsuarioDAL>();
            services.AddScoped<IUsuarioBLL, UsuarioBLL>();
            services.AddScoped<IAutorizacionDAL, AutorizacionDAL>();
            services.AddScoped<IAutorizacionBLL, AutorizacionBLL>();
            services.AddScoped<IPlantillaCorreoDAL, PlantillaCorreoDAL>();
            services.AddScoped<IPlantillasCorreoService, PlantillasCorreoService>();
            services.Configure<ServidorEmail>(configuration.GetSection("ServidorEmail"));

            //Guardar en memoria Caché las Plantillas de los correos
            services.AddMemoryCache();

            //Capturar la URL del servidor dentro del método de la clase de una biblioteca de clases
            services.AddHttpContextAccessor();

            //Implementación del Automapper (de los modelos de BD a los DTO y viceversa)
            services.AddAutoMapper(typeof(AutoMapperProfile));

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
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true, //valida q las apps externas puedan usar la URL donde se encuentra nuestra Api
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidateAudience = true, //quienes pueden acceder a nuestra Api
                    ValidAudience = configuration["JwtSettings:Audience"],
                    ValidateLifetime = true, //valida el tiempo de vida del Token
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]!)
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

                        //Si no está en el header, busca como tal en la Cookie
                        if (string.IsNullOrEmpty(token))
                            token = context.Request.Cookies["cookieAccessToken"];
                        
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
                        "http://localhost:4200",  //Desarrollo
                        "https://miwebapp.com"  //Producción
                     )
                    .AllowCredentials() //permite cargar las cookies en el navegador
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

        }

        public static void ConfigurarInicializarAplicacionWeb(this IServiceCollection services, WebApplication app)
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

            app.UseSession();
            app.UseMiddleware<SessionTimeoutMiddleware>();

            //app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

    }
}
