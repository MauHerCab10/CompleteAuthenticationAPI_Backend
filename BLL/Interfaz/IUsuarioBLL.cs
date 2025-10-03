using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.DTOs;
using Transversal.Enums;
using Transversal.Model;

namespace BLL.Interfaz
{
    public interface IUsuarioBLL
    {
        Task<Respuesta<Usuario>> ConsultarUsuario(string email, string? contrasena = null);
        
        Task<Respuesta<Usuario>> AutenticarUsuario(Usuario pUsuario);
        
        Task<Respuesta<Usuario>> RegistrarUsuario(Usuario pUsuario);
        
        Task<Respuesta<Usuario>> ReestablecerContrasena(string email);
        
        Task<Respuesta<Usuario>> ActualizarContrasenaAntigua(string guidAcceso, string nuevaContrasena, string confirmacionContrasena);
        
        Task<bool> ConfirmarCuenta(string guidAcceso);
        
        Task<PlantillaCorreo> ObtenerPlantillaPorEnum(PlantillasCorreoEnum tipoPlantilla);
    }
}
