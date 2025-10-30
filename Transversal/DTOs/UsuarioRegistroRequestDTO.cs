using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transversal.DTOs
{
    public class UsuarioRegistroRequestDTO
    {        
        public string NombreApellido { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public string Contrasena { get; set; } = string.Empty;
    }
}
