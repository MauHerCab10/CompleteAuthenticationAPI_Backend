using DAL.Interfaz;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

namespace DAL.Implementacion
{
    public class UsuarioDAL : IUsuarioDAL
    {
        private readonly string cadenaConexion;
        public UsuarioDAL(IConfiguration configuration)
        {
            cadenaConexion = configuration.GetConnectionString("ConexionSQL") ?? "";
        }

        public async Task<bool> RegistrarUsuario(Usuario usuario)
        {
            bool respuesta = false;

            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                using (SqlCommand command = new SqlCommand("sp_InsertarUsuario", connection))
                {
                    try
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@NombreApellido", usuario.NombreApellido);
                        command.Parameters.AddWithValue("@Email", usuario.Email);
                        command.Parameters.AddWithValue("@ContrasenaHash", usuario.ContrasenaHash);
                        command.Parameters.AddWithValue("@Restablecer", usuario.Restablecer);
                        command.Parameters.AddWithValue("@Confirmado", usuario.Confirmado);
                        command.Parameters.AddWithValue("@GuidAcceso", usuario.GuidAcceso);

                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();

                        int regsAfectados = await command.ExecuteNonQueryAsync();

                        if (regsAfectados > 0)
                            respuesta = true;
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

                return respuesta;
            }
        }

        public async Task<Usuario> ConsultarUsuario(string email, string? contrasenaHash = null)
        {
            Usuario? usuario = null;

            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                using (SqlCommand command = new SqlCommand("sp_ConsultarUsuario", connection))
                {
                    try
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@ContrasenaHash", contrasenaHash);

                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();

                        using (SqlDataReader dr = await command.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                usuario = new Usuario
                                {
                                    IdUsuario = Convert.ToInt32(dr["IdUsuario"].ToString()),
                                    NombreApellido = dr["NombreApellido"].ToString() ?? "",
                                    Email = dr["Email"].ToString() ?? "",
                                    ContrasenaHash = dr["ContrasenaHash"].ToString() ?? "",
                                    Restablecer = Convert.ToBoolean(dr["Restablecer"].ToString()),
                                    Confirmado = Convert.ToBoolean(dr["Confirmado"].ToString()),
                                    GuidAcceso = dr["GuidAcceso"].ToString() ?? ""
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

                return usuario!;
            }
        }

        public async Task<bool> ReestablecerContrasena(int restablecer, int confirmado, string contrasenaHash, string guidAcceso)
        {
            bool respuesta = false;

            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                using (SqlCommand command = new SqlCommand("sp_RestablecerContrasena", connection))
                {
                    try
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@ContrasenaHash", contrasenaHash);
                        command.Parameters.AddWithValue("@Restablecer", restablecer);
                        command.Parameters.AddWithValue("@Confirmado", confirmado);
                        command.Parameters.AddWithValue("@GuidAcceso", guidAcceso);

                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();

                        int regsAfectados = await command.ExecuteNonQueryAsync();

                        if (regsAfectados > 0)
                            respuesta = true;
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

                return respuesta;
            }
        }

        public async Task<bool> ConfirmarCuenta(string guidAcceso)
        {
            bool respuesta = false;

            using (SqlConnection cnn = new SqlConnection(cadenaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ConfirmarCuenta", cnn))
                {
                    try
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@GuidAcceso", guidAcceso);

                        if (cnn.State == ConnectionState.Closed)
                            cnn.Open();

                        int regsAfectados = await cmd.ExecuteNonQueryAsync();

                        if (regsAfectados > 0)
                            respuesta = true;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    finally
                    {
                        if (cnn.State == ConnectionState.Open)
                            await cnn.CloseAsync();
                    }
                }

                return respuesta;
            }
        }

    }
}
