using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transversal.DTOs
{
    public class UsuarioLoginRequestDTO
    {
        public required string Email { get; set; }

        public required string Contrasena { get; set; }
    }
}
