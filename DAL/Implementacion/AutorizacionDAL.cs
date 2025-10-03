using Azure.Core;
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
    public class AutorizacionDAL : IAutorizacionDAL
    {
        private readonly string cadenaConexion;

        public AutorizacionDAL(IConfiguration configuration)
        {
            cadenaConexion = configuration.GetConnectionString("ConexionSQL") ?? "";
        }

        public async Task<HistorialRefreshToken> ConsultarUltimoHistorialRefreshTokensPorUsuario(int idUsuario, string? accessToken, string? refreshToken)
        {
            HistorialRefreshToken ultimoAcceso = null!;

            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                using (SqlCommand command = new SqlCommand("sp_ConsultarUltimoHistorialRefreshTokensPorUsuario", connection))
                {
                    try
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        command.Parameters.AddWithValue("@AccessToken", idUsuario);
                        command.Parameters.AddWithValue("@RefreshToken", idUsuario);

                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();

                        using (SqlDataReader dr = await command.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                ultimoAcceso = new HistorialRefreshToken
                                {
                                    IdHistorialToken = Convert.ToInt32(dr["IdHistorialToken"].ToString()),
                                    IdUsuario = Convert.ToInt32(dr["IdUsuario"].ToString()),
                                    AccessToken = dr["AccessToken"].ToString()!,
                                    RefreshToken = dr["RefreshToken"].ToString()!,
                                    FechaCreacion = Convert.ToDateTime(dr["FechaCreacion"].ToString()),
                                    FechaExpiracion = Convert.ToDateTime(dr["FechaExpiracion"].ToString()),
                                    EstaActivo = Convert.ToBoolean(dr["EstaActivo"].ToString())
                                };
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

                return ultimoAcceso;
            }
        }

        public async Task<int> GuardarHistorialRefreshTokenDeUsuario(int idUsuario, string accessToken, string refreshToken, DateTime fechaCreacion, DateTime fechaExpiracion)
        {
            object nuevoIdHistorialToken;

            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                using (SqlCommand command = new SqlCommand("sp_GuardarHistorialRefreshTokenDeUsuario", connection))
                {
                    try
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        command.Parameters.AddWithValue("@AccessToken", accessToken);
                        command.Parameters.AddWithValue("@RefreshToken", refreshToken);
                        command.Parameters.AddWithValue("@FechaCreacion", fechaCreacion);
                        command.Parameters.AddWithValue("@FechaExpiracion", fechaExpiracion);

                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();

                        nuevoIdHistorialToken = await command.ExecuteScalarAsync() ?? 0;
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

                return Convert.ToInt32(nuevoIdHistorialToken);
            }
        }

        public async Task<bool> ActualizarHistorialRefreshTokenDeUsuario(int idHistorialToken, string accessToken)
        {
            bool resultado;

            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                using (SqlCommand command = new SqlCommand("sp_ActualizarHistorialRefreshTokenDeUsuario", connection))
                {
                    try
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@IdHistorialToken", idHistorialToken);
                        command.Parameters.AddWithValue("@AccessToken", accessToken);

                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();

                        resultado = await command.ExecuteNonQueryAsync() > 0;
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

                return resultado;
            }
        }

        public async Task<bool> EliminarHistorialRefreshTokensPorUsuario(int idUsuario)
        {
            bool resultado;

            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                using (SqlCommand command = new SqlCommand("sp_EliminarHistorialRefreshTokensPorUsuario", connection))
                {
                    try
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@IdUsuario", idUsuario);

                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();

                        resultado = await command.ExecuteNonQueryAsync() > 0;
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

                return resultado;
            }
        }

    }
}