using Isopoh.Cryptography.Argon2;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using MimeKit.Text;
using Service.Interfaz;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Transversal.Model;

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
            // Generar un GUID único
            string guid = Guid.NewGuid().ToString("N"); //N = texto sin guiones

            // Agregar un componente variable (marca de tiempo)
            string data = guid + DateTime.Now.Ticks;

            // Crear un hash SHA256
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                byte[] hash = sha.ComputeHash(bytes);

                // Convertir el hash a una cadena hexadecimal
                StringBuilder tokenBuilder = new StringBuilder();
                foreach (byte b in hash)
                {
                    tokenBuilder.Append(b.ToString("x2"));
                }

                string token = tokenBuilder.ToString();  //Token final
                return token;
            }
        }

        //public string EncriptarContraseña(string contrasena) //método anterior solo con "Hash SHA-256"
        //{
        //    using (SHA256 sha256 = SHA256.Create())
        //    {
        //        StringBuilder sb = new StringBuilder();
        //        byte[] bytesContrasena = sha256.ComputeHash(Encoding.UTF8.GetBytes(contrasena));

        //        foreach (byte b in bytesContrasena)
        //            sb.Append(b.ToString("x2")); //El formato "x2" convierte cada byte del array en su representación hexadecimal. La "x" indica el formato hexadecimal y el "2" asegura que cada valor tenga al menos dos caracteres, añadiendo un cero a la izquierda si es necesario

        //        //Contraseña encriptada
        //        string contrasenaEncriptada = sb.ToString();
        //        return contrasenaEncriptada;
        //    }
        //}

        // Encriptar la Contraseña generando un Hash seguro [Hashing con sal + algoritmo lento (PBKDF2, bcrypt o Argon2)] (Argon2 es recomendado por OWASP y NIST)
        public string EncriptarContraseña(string contrasena)
        {
            // Salt aleatorio 16 bytes
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            var config = new Argon2Config
            {
                Type = Argon2Type.HybridAddressing, //Argon2 → más seguro
                Version = Argon2Version.Nineteen,
                TimeCost = 4, //Iteraciones (ajusta según el rendimiento del servidor)
                MemoryCost = 1024 * 64, //64 MB de RAM (recomendado)
                Lanes = 4, //Número de hilos paralelos
                Threads = Environment.ProcessorCount,
                Salt = salt,
                Password = Encoding.UTF8.GetBytes(contrasena),
                HashLength = 32 //256 bits de salida
            };

            string encodedHash = Argon2.Hash(config);
            return encodedHash;
        }

        // Verificar una contraseña con el Hash con sal guardada
        public bool VerificarContrasena(string contrasena, string contrasenaHashGuardada)
        {
            return Argon2.Verify(contrasenaHashGuardada, contrasena);
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
