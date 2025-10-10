using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaz
{
    public interface ICookieService
    {
        void SetCookieAccessToken(string token);

        void SetCookieRefreshToken(string token);

        void EliminarCookiesDelUsuario();
    }
}
