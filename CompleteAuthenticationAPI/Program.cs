
using AutoMapper;
using BLL.Implementacion;
using BLL.Interfaz;
using CompleteAuthenticationAPI.Middleware;
using DAL.Implementacion;
using DAL.Interfaz;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Service.Implementacion;
using Service.Interfaz;
using System.Text;
using Transversal.Helper;
using Transversal.Model;
using Transversal.Service;

namespace CompleteAuthenticationAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            // Configurar sesiones
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Timeout de respaldo
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });





            //Inyección de Dependencias
            builder.Services.AddSingleton<IUtilidades, Utilidades>();
            builder.Services.AddScoped<IUsuarioDAL, UsuarioDAL>();
            builder.Services.AddScoped<IUsuarioBLL, UsuarioBLL>();
            builder.Services.AddScoped<IPlantillaCorreoDAL, PlantillaCorreoDAL>();
            builder.Services.AddScoped<IPlantillasCorreoService, PlantillasCorreoService>();
            builder.Services.Configure<ServidorEmail>(builder.Configuration.GetSection("ServidorEmail"));

            //Guardar en memoria Caché las Plantillas de los correos
            builder.Services.AddMemoryCache();

            //Capturar la URL del servidor dentro del método de la clase de una biblioteca de clases
            builder.Services.AddHttpContextAccessor();

            //Implementación del Automapper (de los modelos de BD a los DTO y viceversa)
            builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

            //JSON Web Token (JWT) configuration
            builder.Services.AddAuthentication(configuration =>
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
                    (Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"]!))
                };
            });

            //Habilitar CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("PolicyCORS", app =>
                {
                    app
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });





            //Inyección de Dependencias
            //builder.Services.InyectarDependencias(builder.Configuration);



            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("PolicyCORS");
            app.UseAuthentication();

            app.UseSession();
            app.UseMiddleware<SessionTimeoutMiddleware>();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
