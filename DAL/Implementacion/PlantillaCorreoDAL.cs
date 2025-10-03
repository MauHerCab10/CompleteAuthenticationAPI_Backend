using DAL.Interfaz;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

namespace DAL.Implementacion
{
    public class PlantillaCorreoDAL : IPlantillaCorreoDAL
    {
        private readonly string cadenaConexion;
        public PlantillaCorreoDAL(IConfiguration configuration)
        {
            cadenaConexion = configuration.GetConnectionString("ConexionSQL") ?? "";
        }

        public async Task<List<PlantillaCorreo>> ObtenerPlantillasCorreo()
        {
            List<PlantillaCorreo>? correoPlantillas = null;

            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                using (SqlCommand command = new SqlCommand("sp_ObtenerPlantillasCorreo", connection))
                {
                    try
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();

                        using (SqlDataReader dr = await command.ExecuteReaderAsync())
                        {
                            correoPlantillas = new List<PlantillaCorreo>();

                            while (await dr.ReadAsync())
                            {
                                correoPlantillas.Add(new PlantillaCorreo
                                {
                                    Nombre = dr["NombrePlantillaCorreo"].ToString() ?? "",
                                    Asunto = dr["AsuntoPlantillaCorreo"].ToString() ?? "",
                                    Cuerpo = dr["CuerpoPlantillaCorreo"].ToString() ?? ""
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                            await connection.CloseAsync();
                    }
                }

                return correoPlantillas;
            }
        }

    }
}
