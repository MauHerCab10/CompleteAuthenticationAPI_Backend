using DAL.Implementacion;
using DAL.Interfaz;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Service.Interfaz;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

namespace Service.Implementacion
{
    public class PlantillasCorreoService : IPlantillasCorreoService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IPlantillaCorreoDAL _correoPlantillaDAL;

        public PlantillasCorreoService(IConfiguration configuration, IMemoryCache cache, IPlantillaCorreoDAL correoPlantillaDAL)
        {
            _configuration = configuration;
            _cache = cache;
            _correoPlantillaDAL = correoPlantillaDAL;
        }

        public async Task<List<PlantillaCorreo>> CargarPlantillasCorreoDesdeDB()
        {
            if (!_cache.TryGetValue(_configuration["Plantillas_Correos_Cache_Key"]!, out List<PlantillaCorreo>? plantillas))
            {
                plantillas = await _correoPlantillaDAL.ObtenerPlantillasCorreo();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), //crear un archivo de recursos (.resx) para este tipo de valores quemados
                    Priority = CacheItemPriority.High
                };

                _cache.Set(_configuration["Plantillas_Correos_Cache_Key"]!, plantillas, cacheOptions);
            }

            return plantillas ?? new List<PlantillaCorreo>();
        }

    }
}
