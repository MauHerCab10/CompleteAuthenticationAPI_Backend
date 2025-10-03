using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transversal.Model
{
    public class HistorialRefreshToken
    {
        public int IdHistorialToken { get; set; }

        public int IdUsuario { get; set; }

        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }

        public DateTime FechaExpiracion { get; set; }

        public bool EstaActivo { get; set; }
    }
}
