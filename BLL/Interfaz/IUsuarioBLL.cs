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
        Task<Respuesta<Usuario>> ConsultarUsuarioPorGuid(string guidUsuario);

        Task<Respuesta<Usuario>> ConsultarUsuarioPorId(string email); //string? contrasena = null

        Task<Respuesta<Usuario>> AutenticarUsuario(Usuario pUsuario);
        
        Task<Respuesta<Usuario>> RegistrarUsuario(Usuario pUsuario);
        
        Task<Respuesta<Usuario>> OlvidoSuContrasena(string email);
                
        Task<Respuesta<Usuario>> ActualizarContrasenaAntigua(string guidAcceso, string nuevaContrasena, string confirmacionContrasena);

        Task<Respuesta<Usuario>> ConfirmarCuenta(string guidAcceso);
        
        Task<PlantillaCorreo> ObtenerPlantillaPorEnum(PlantillasCorreoEnum tipoPlantilla);
    }
}
