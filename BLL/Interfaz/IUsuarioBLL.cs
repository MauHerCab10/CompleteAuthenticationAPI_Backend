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
        Task<Respuesta<UsuarioResponseDTO>> AutenticarUsuario(UsuarioLoginRequestDTO dtoUsuario);
        
        Task<Respuesta<UsuarioResponseDTO>> RegistrarUsuario(UsuarioRegistroRequestDTO dtoUsuario);
        
        Task<Respuesta<UsuarioResponseDTO>> OlvidoSuContrasena(string email);
                
        Task<Respuesta<UsuarioResponseDTO>> ActualizarContrasenaAntigua(string guidAcceso, string nuevaContrasena, string confirmacionContrasena);

        Task<Respuesta<UsuarioResponseDTO>> ConfirmarCuenta(string guidAcceso);
    }
}
