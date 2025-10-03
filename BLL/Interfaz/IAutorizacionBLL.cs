using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.DTOs;
using Transversal.Model;

namespace Service.Interfaz
{
    public interface IAutorizacionBLL
    {
        Task<Respuesta<Usuario>> GenerarAccessTokenYRefreshTokenConCredenciales(LoginUsuarioDTO autorizacion);

        Task<Respuesta<Usuario>> GenerarAccessTokenYRefreshTokenConRefreshTokenAnterior(int idUsuario, string accessToken, string refreshToken);

        Task<DateTime?> ConsultarFechaVencimientoRefreshToken(int idUsuario);

        Task<Respuesta<Usuario>> EliminarHistorialRefreshTokenAnteriores(int idUsuario);

        Task<Respuesta<Usuario>> ActualizarAccessTokenConRefreshTokenAnterior(int idUsuario, string accessToken, string refreshToken);

        Task<Respuesta<Usuario>> CerrarSesion(int idUsuario);

        bool ValidarToken(string token);
    }
}
