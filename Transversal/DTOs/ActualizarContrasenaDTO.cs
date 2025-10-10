using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transversal.DTOs
{
    public class ActualizarContrasenaDTO
    {
        public required string GuidAcceso { get; set; }
        
        public required string NuevaContrasena { get; set; }
        
        public required string ConfirmacionContrasena { get; set; }

    }
}
