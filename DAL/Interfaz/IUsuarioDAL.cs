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

        Task<Usuario> ConsultarUsuario(string email, string? contrasenaHash = null);

        Task<bool> ReestablecerContrasena(int restablecer, int confirmado, string contrasenaHash, string guidAcceso);

        Task<bool> ConfirmarCuenta(string guidAcceso);
    }
}
