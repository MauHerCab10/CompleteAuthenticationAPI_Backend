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

        Task<Usuario> ConsultarUsuarioPorId(string email);

        Task<bool> RestablecerContrasena(Usuario usuarioRestablecido);

        Task<bool> ActualizarContrasenaAntigua(string guidAcceso, string contrasenaHash);

        Task<bool> ConfirmarCuenta(string guidAcceso);
    }
}
