using Transversal.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Service.Interfaz;

namespace Transversal.Service
{
    public class Utilidades : IUtilidades
    {
        private readonly IConfiguration _configuration;
        private readonly ServidorEmail _emailInfo;

        public Utilidades(IConfiguration configuration, IOptions<ServidorEmail> options)
        {
            _configuration = configuration;
            _emailInfo = options.Value;
        }

        public string GenerarGuid()
        {
            string token = Guid.NewGuid().ToString("N");
            return token;
        }

        public string EncriptarContraseña(string contrasena)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                StringBuilder sb = new StringBuilder();
                byte[] bytesContrasena = sha256.ComputeHash(Encoding.UTF8.GetBytes(contrasena));

                foreach (byte b in bytesContrasena)
                    sb.Append(b.ToString("x2")); //El formato "x2" convierte cada byte del array en su representación hexadecimal. La "x" indica el formato hexadecimal y el "2" asegura que cada valor tenga al menos dos caracteres, añadiendo un cero a la izquierda si es necesario

                //Contraseña encriptada
                string contrasenaEncriptada = sb.ToString();
                return contrasenaEncriptada;
            }
        }

        public bool EnviarCorreo(InfoCorreo request)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_emailInfo.Username));
                email.To.Add(MailboxAddress.Parse(request.Para));
                email.Subject = request.Asunto;
                email.Body = new TextPart(TextFormat.Html)
                {
                    Text = request.Contenido,
                };

                using var smtp = new SmtpClient();
                smtp.Connect(_emailInfo.Host, Convert.ToInt32(_emailInfo.Port), SecureSocketOptions.StartTls);
                smtp.Authenticate(_emailInfo.Username, _emailInfo.Password);
                smtp.Send(email);
                smtp.Disconnect(true);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // Obtiene la hora actual en zona horaria de Colombia
        public DateTime FechaHoraActualColombia()
        {
            var colombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_configuration.GetValue<string>("JwtSettings:TimeZone")!);
            var nowColombia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, colombiaTimeZone);

            return nowColombia;
        }

    }
}
