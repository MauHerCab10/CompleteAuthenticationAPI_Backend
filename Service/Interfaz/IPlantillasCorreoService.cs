using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

namespace Service.Interfaz
{
    public interface IPlantillasCorreoService
    {
        Task<List<PlantillaCorreo>> CargarPlantillasCorreoDesdeDB();
    }
}
