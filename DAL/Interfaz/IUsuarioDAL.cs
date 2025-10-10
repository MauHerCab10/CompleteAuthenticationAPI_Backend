using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

namespace DAL.Interfaz
{
    public interface IUsuarioDAL
    {
        Task<bool> RegistrarUsuario(Usuario usuario);

        Task<Usuario> ConsultarUsuarioPorGuid(string guidUsuario);

        Task<Usuario> ConsultarUsuarioPorId(string email); //string? contrasenaHash = null

        Task<bool> RestablecerContrasena(int idUsuario, string newGuidAcceso, DateTime fechaCreacionGuid, DateTime fechaExpiracionGuid);

        Task<bool> ActualizarContrasenaAntigua(string guidAcceso, string contrasenaHash);

        Task<bool> ConfirmarCuenta(string guidAcceso);
    }
}
