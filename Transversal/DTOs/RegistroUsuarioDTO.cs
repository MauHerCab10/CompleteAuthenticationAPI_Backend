using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transversal.DTOs
{
    public class RegistroUsuarioDTO
    {
        public int IdUsuario { get; set; }
        
        public string NombreApellido { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public string Contrasena { get; set; } = string.Empty;
        
        public string ContrasenaHash { get; set; } = string.Empty;
        
        public bool Restablecer { get; set; }
        
        public bool Confirmado { get; set; }
        
        public string GuidAcceso { get; set; } = string.Empty;
        
        public string Token { get; set; } = string.Empty;
    }
}
