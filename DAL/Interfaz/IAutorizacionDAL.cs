using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

namespace DAL.Interfaz
{
    public interface IAutorizacionDAL
    {
        Task<HistorialRefreshToken> ConsultarUltimoHistorialRefreshTokensPorUsuario(int idUsuario, string? accessToken, string? refreshToken);

        Task<int> GuardarHistorialRefreshTokenDeUsuario(int idUsuario, string accessToken, string refreshToken, DateTime fechaCreacion, DateTime fechaExpiracion);

        Task<bool> ActualizarHistorialRefreshTokenDeUsuario(int idHistorialToken, string accessToken);

        Task<bool> EliminarHistorialRefreshTokensPorUsuario(int idUsuario);
    }
}
