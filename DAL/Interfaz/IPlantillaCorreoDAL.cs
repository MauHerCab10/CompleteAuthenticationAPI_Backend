using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

namespace DAL.Interfaz
{
    public interface IPlantillaCorreoDAL
    {
        Task<List<PlantillaCorreo>> ObtenerPlantillasCorreo();
    }
}
