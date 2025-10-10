using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

namespace Service.Interfaz
{
    public interface IUtilidades
    {
        string GenerarGuid();
        
        string EncriptarContraseña(string contrasena);

        bool VerificarContrasena(string contrasena, string contrasenaHashGuardada);


        bool EnviarCorreo(InfoCorreo request);

        DateTime FechaHoraActualColombia();
    }
}
