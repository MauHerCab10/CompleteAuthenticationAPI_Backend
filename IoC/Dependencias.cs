using BLL.Implementacion;
using BLL.Interfaz;
using DAL.Implementacion;
using DAL.Interfaz;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Service.Implementacion;
using Service.Interfaz;
using System;
using System.Collections.Generic;
using System.Linq;
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
            //Inyección de Dependencias
            services.AddSingleton<IUtilidades, Utilidades>();
            services.AddScoped<IUsuarioDAL, UsuarioDAL>();
            services.AddScoped<IUsuarioBLL, UsuarioBLL>();
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
            services.AddAuthentication(configuration =>
            {
                configuration.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                configuration.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(jwtConfig =>
            {
                jwtConfig.RequireHttpsMetadata = false;
                jwtConfig.SaveToken = true;
                jwtConfig.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false, //valida q las apps externas puedan usar la URL donde se encuentra nuestra Api
                    ValidateAudience = false, //quienes pueden acceder a nuestra Api
                    ValidateLifetime = true, //valida el tiempo de vida del Token
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey
                    (Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]!))
                };
            });

            //Habilitar CORS
            services.AddCors(options =>
            {
                options.AddPolicy("PolicyCORS", app =>
                {
                    app
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });
        }

    }
}
