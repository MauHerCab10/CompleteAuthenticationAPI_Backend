using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transversal.Model
{
    public class Respuesta<T>
    {
        public bool IsSuccess { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public T Objeto { get; set; } = default!;
    }
}
