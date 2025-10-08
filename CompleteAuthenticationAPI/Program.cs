
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

            //Inyección y Configuración de Dependencias
            builder.Services.InyectarDependencias(builder.Configuration);

            //Configuración e Inicialización de la Aplicación Web
            builder.Services.ConfigurarInicializarAplicacionWeb(builder.Build());
        }
    }
}
