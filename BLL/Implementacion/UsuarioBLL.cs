using AutoMapper;
using BLL.Interfaz;
using DAL.Implementacion;
using DAL.Interfaz;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Service.Interfaz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Transversal.DTOs;
using Transversal.Enums;
using Transversal.Model;
using Transversal.Service;

namespace BLL.Implementacion
{
    public class UsuarioBLL : IUsuarioBLL
    {
        private readonly IConfiguration _configuration;
        private readonly IAutorizacionBLL _autorizacionBLL;
        private readonly IUtilidades _utilidades;
        private readonly IUsuarioDAL _usuarioDAL;
        private readonly IPlantillasCorreoService _plantillaCorreo;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuarioBLL(IConfiguration configuration, IAutorizacionBLL autorizacionBLL, IUtilidades utilidades, IUsuarioDAL usuarioDAL, IPlantillasCorreoService plantillaCorreo, IMapper mapper, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _autorizacionBLL = autorizacionBLL;
            _utilidades = utilidades;
            _usuarioDAL = usuarioDAL;
            _plantillaCorreo = plantillaCorreo;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Respuesta<Usuario>> ConsultarUsuarioPorGuid(string guidUsuario)
        {
            try
            {
                Respuesta<Usuario> resultOperacion = new Respuesta<Usuario>
                {
                    Objeto = await _usuarioDAL.ConsultarUsuarioPorGuid(guidUsuario)
                };

                if (resultOperacion.Objeto == null || !resultOperacion.Objeto.GuidActivo)
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "GUID no existe o ya se encuentra inválido. Favor solicite el reestablecimiento de contraseña nuevamente." };
                else
                    return new Respuesta<Usuario> { IsSuccess = true, Objeto = resultOperacion.Objeto, Mensaje = "¡GUID existe en la BD!" };
            }
            catch (Exception e)
            {
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        public async Task<Respuesta<Usuario>> ConsultarUsuarioPorId(string email) //string? contrasena = null
        {
            try
            {
                Respuesta<Usuario> resultOperacion = new Respuesta<Usuario>
                {
                    Objeto = await _usuarioDAL.ConsultarUsuarioPorId(email) //contrasena != null ? _utilidades.EncriptarContraseña(contrasena) : null
                };

                if (resultOperacion.Objeto == null)
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "Usuario no encontrado. Favor validar los datos ingresados." }; //usado para "AutenticarUsuario"
                else
                    return new Respuesta<Usuario> { IsSuccess = true, Objeto = resultOperacion.Objeto, Mensaje = "¡Usuario existe en la BD!" }; //usado para "RegistrarUsuario"
            }
            catch (Exception e)
            {
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        public async Task<Respuesta<Usuario>> AutenticarUsuario(Usuario pUsuario)
        {
            try
            {
                Respuesta<Usuario> resultOperacion = new Respuesta<Usuario>
                {
                    Objeto = await _usuarioDAL.ConsultarUsuarioPorId(pUsuario.Email) //_utilidades.EncriptarContraseña(pUsuario.Contrasena)
                };

                if (resultOperacion.Objeto != null)
                {
                    bool contrasenaValidada = _utilidades.VerificarContrasena(pUsuario.Contrasena, resultOperacion.Objeto.ContrasenaHash);

                    if (!resultOperacion.Objeto.Confirmado)
                    {
                        return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"Falta confirmar su cuenta. Se le envió un correo a {pUsuario.Email}." };
                    }
                    else if (resultOperacion.Objeto.Restablecer)
                    {
                        return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"Se ha solicitado restablecer su cuenta. Favor revise la bandeja de su correo {pUsuario.Email}." };
                    }
                    else if (!contrasenaValidada)
                    {
                        return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "La contraseña no coincide con la que hay almacenada en el sistema." };
                    }
                    else
                    {
                        resultOperacion = await _autorizacionBLL.GenerarAccessTokenYRefreshTokenConCredenciales(pUsuario.Email); //Contrasena = _utilidades.EncriptarContraseña(pUsuario.Contrasena) //aqui debo encargarme de enviar un objeto tipo LoginUsuarioDTO
                        return new Respuesta<Usuario> { IsSuccess = true, Objeto = resultOperacion.Objeto, Mensaje = "¡Autenticación exitosa!" };
                    }
                }
                else
                {
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "No se encontraron coincidencias con esas credenciales." };
                }
            }
            catch (Exception e)
            {
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        public async Task<Respuesta<Usuario>> RegistrarUsuario(Usuario pUsuario)
        {
            try
            {
                var existeUsuario = await ConsultarUsuarioPorId(pUsuario.Email); //_utilidades.EncriptarContraseña(pUsuario.Contrasena)

                if (existeUsuario.IsSuccess)
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"El correo electrónico proporcionado ya se encuentra registrado en el sistema. {existeUsuario.Mensaje}" };

                if (string.IsNullOrEmpty(pUsuario.NombreApellido))
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "campo de Nombre y Apellido es obligatorio." };

                if (!Regex.IsMatch(pUsuario.Email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "Formato de correo electrónico inválido." };

                if (pUsuario.Contrasena.Length < 12 || !Regex.IsMatch(pUsuario.Contrasena, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[^\s]+$"))
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "Formato de contraseña inválido. La contraseña debe contener 12 caracteres como mínimo, al menos una minúscula, una mayúscula, un número, un caracter especial y no debe contener espacios." };


                pUsuario.ContrasenaHash = _utilidades.EncriptarContraseña(pUsuario.Contrasena);
                pUsuario.GuidAcceso = _utilidades.GenerarGuid();
                pUsuario.Restablecer = false;
                pUsuario.Confirmado = false;

                Usuario usuario = new Usuario
                {
                    NombreApellido = pUsuario.NombreApellido,
                    Email = pUsuario.Email,
                    ContrasenaHash = pUsuario.ContrasenaHash,
                    Restablecer = pUsuario.Restablecer,
                    Confirmado = pUsuario.Confirmado,
                    GuidAcceso = pUsuario.GuidAcceso,
                    FechaCreacionGuid = _utilidades.FechaHoraActualColombia(),
                    FechaExpiracionGuid = _utilidades.FechaHoraActualColombia().AddMinutes(_configuration.GetValue<int>("GuidAcceso_ExpirationTime"))
                };

                var respuesta = await _usuarioDAL.RegistrarUsuario(usuario);

                if (respuesta)
                {
                    PlantillaCorreo? plantillaCorreo = await ObtenerPlantillaPorEnum(PlantillasCorreoEnum.ConfirmarCorreo);

                    HttpRequest urlHost = _httpContextAccessor.HttpContext!.Request;
                    string url = $"{urlHost.Scheme}://{urlHost.Host}{urlHost.PathBase}{$"/api/Usuario/ConfirmarCuenta?guidAcceso={pUsuario.GuidAcceso}"}";

                    string htmlBody = string.Format(plantillaCorreo.Cuerpo, pUsuario.NombreApellido, url);

                    InfoCorreo infoCorreo = new InfoCorreo()
                    {
                        Para = pUsuario.Email,
                        Asunto = plantillaCorreo.Asunto,
                        Contenido = htmlBody
                    };

                    bool correoEnviado = _utilidades.EnviarCorreo(infoCorreo);

                    if (correoEnviado)
                        return new Respuesta<Usuario> { IsSuccess = true, Mensaje = $"Su cuenta ha sido creada satisfactoriamente. Hemos enviado un mensaje al correo '{pUsuario.Email}' para confirmar su cuenta." };
                    else
                        return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"¡ERROR! No fue posible enviar el correo a '{pUsuario.Email}'." };
                }
                else
                {
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"¡ERROR! No se pudo crear su cuenta." };
                }
            }
            catch (Exception e)
            {
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        public async Task<Respuesta<Usuario>> OlvidoSuContrasena(string email)
        {
            try
            {
                var usuarioEncontrado = await ConsultarUsuarioPorId(email);
                if (usuarioEncontrado.IsSuccess)
                {
                    string newGuidAcceso = _utilidades.GenerarGuid();
                    DateTime fechaCreacionGuid = _utilidades.FechaHoraActualColombia();
                    DateTime fechaExpiracionGuid = _utilidades.FechaHoraActualColombia().AddMinutes(_configuration.GetValue<int>("JwtSettings:GuidAcceso_ExpirationTime"));

                    bool respuesta = await _usuarioDAL.RestablecerContrasena(usuarioEncontrado.Objeto.IdUsuario, newGuidAcceso, fechaCreacionGuid, fechaExpiracionGuid);
                    if (respuesta)
                    {
                        PlantillaCorreo? plantillaCorreo = await ObtenerPlantillaPorEnum(PlantillasCorreoEnum.RestablecerContrasena);

                        HttpRequest urlHost = _httpContextAccessor.HttpContext!.Request;
                        string url = $"{urlHost.Scheme}://{urlHost.Host}{urlHost.PathBase}{$"/api/Usuario/RestablecerContrasena?guidAcceso={newGuidAcceso}"}";

                        string htmlBody = string.Format(plantillaCorreo.Cuerpo, usuarioEncontrado.Objeto.NombreApellido, url);

                        InfoCorreo correoDTO = new InfoCorreo()
                        {
                            Para = usuarioEncontrado.Objeto.Email,
                            Asunto = "Restablecer contraseña",
                            Contenido = htmlBody
                        };

                        bool correoEnviado = _utilidades.EnviarCorreo(correoDTO);

                        if (correoEnviado)
                            return new Respuesta<Usuario> { IsSuccess = true, Mensaje = "La solicitud de reestablecimiento de contraseña fue procesada satisfactoriamente. Por favor revise la bandeja de entrada de su correo electrónico para actualizar su contraseña." };
                        else
                            return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"¡ERROR! No fue posible restablecer su contraseña." };
                    }
                    else
                    {
                        return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"¡ERROR! No se pudo restablecer su cuenta." };
                    }
                }
                else
                {
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"No se encontraron coincidencias con el correo proporcionado." };
                }
            }
            catch (Exception e)
            {
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        public async Task<Respuesta<Usuario>> ActualizarContrasenaAntigua(string guidAcceso, string nuevaContrasena, string confirmacionContrasena)
        {
            try
            {
                if (nuevaContrasena != confirmacionContrasena)
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "Las contraseñas ingresadas no coinciden." };

                if (nuevaContrasena.Length < 12 || !Regex.IsMatch(nuevaContrasena, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[^\s]+$"))
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "Formato de contraseña inválido. La contraseña debe contener 12 caracteres como mínimo, al menos una minúscula, una mayúscula, un número, un caracter especial y no debe contener espacios." };

                string contrasenaEncriptada = _utilidades.EncriptarContraseña(nuevaContrasena);
                bool respuesta = await _usuarioDAL.ActualizarContrasenaAntigua(guidAcceso, contrasenaEncriptada);

                if (respuesta)
                    return new Respuesta<Usuario> { IsSuccess = true, Mensaje = "¡Contraseña actualizada satisfactoriamente!" };
                else
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "¡ERROR! No se pudo actualizar la contraseña." };
            }
            catch (Exception e)
            {
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        public async Task<Respuesta<Usuario>> ConfirmarCuenta(string guidAcceso)
        {
            try
            {
                bool respuesta = false;
                var existeGuid = await ConsultarUsuarioPorGuid(guidAcceso);

                if (existeGuid.IsSuccess && !existeGuid.Objeto.Confirmado)
                    respuesta = await _usuarioDAL.ConfirmarCuenta(guidAcceso);
                else
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = existeGuid.Mensaje };

                if (respuesta)
                    return new Respuesta<Usuario> { IsSuccess = true, Mensaje = "¡Confirmación de cuenta realizada satisfactoriamente!" };
                else
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"¡ERROR! No se pudo confirmar la cuenta. {existeGuid.Mensaje}" };
            }
            catch (Exception e)
            {
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        public async Task<PlantillaCorreo> ObtenerPlantillaPorEnum(PlantillasCorreoEnum tipoPlantilla)
        {
            List<PlantillaCorreo> plantillas = await _plantillaCorreo.CargarPlantillasCorreoDesdeDB();
            PlantillaCorreo plantilla = plantillas.FirstOrDefault(p => p.Nombre == tipoPlantilla.ToString())!;
            return plantilla;
        }

        //public async Task<ResultadoOperacion> RegistrarUsuario(RegistroUsuarioDTO dtoUsuario)
        //{
        //    try
        //    {
        //        Usuario usuarioModelo = _mapper.Map<Usuario>(dtoUsuario);
        //        var usuarioEncontrado = await _usuarioDAL.ConsultarUsuario(usuarioModelo.NombreUsuario);

        //        if (usuarioEncontrado != null)
        //            return new ResultadoOperacion { IsSuccess = false, Mensaje = "Usuario ya existe." };

        //        if (!Regex.IsMatch(usuarioModelo.Email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
        //            return new ResultadoOperacion { IsSuccess = false, Mensaje = "Formato de correo electrónico inválido." };

        //        if (usuarioModelo.Contrasena.Length < 12 || !Regex.IsMatch(usuarioModelo.Contrasena, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[^\s]+$"))
        //            return new ResultadoOperacion { IsSuccess = false, Mensaje = "Formato de contraseña inválido. La contraseña debe contener 12 caracteres como mínimo, al menos una minúscula, una mayúscula, un número, un caracter especial y no debe contener espacios." };

        //        var nuevoUsuario = new Usuario
        //        {
        //            Nombre = usuarioModelo.Nombre,
        //            Apellido = usuarioModelo.Apellido,
        //            Email = usuarioModelo.Email,
        //            NombreUsuario = usuarioModelo.NombreUsuario,
        //            Contrasena = _utilidades.EncriptarContraseña(usuarioModelo.Contrasena),
        //            IdRol = usuarioModelo.IdRol,
        //            Token = string.Empty
        //        };

        //        bool registro = await _usuarioDAL.RegistrarUsuario(nuevoUsuario);

        //        if (registro)
        //            return new ResultadoOperacion { IsSuccess = true, Mensaje = "¡Usuario creado satisfactoriamente!" };
        //        else
        //            return new ResultadoOperacion { IsSuccess = false, Mensaje = "Error al crear el usuario." };
        //    }
        //    catch (Exception e)
        //    {
        //        return new ResultadoOperacion { IsSuccess = false, Mensaje = e.Message };
        //    }
        //}

    }
}
