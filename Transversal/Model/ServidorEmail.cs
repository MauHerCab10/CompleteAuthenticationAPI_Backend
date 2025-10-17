using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transversal.Model
{
    public class ServidorEmail
    {
        public required string Host { get; set; }

        public required string Port { get; set; }

        public required string Username { get; set; }

        public required string Password { get; set; }
    }
}
