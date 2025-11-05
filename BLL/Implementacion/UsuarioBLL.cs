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

namespace BLL.Implementacion
{
    public class UsuarioBLL : IUsuarioBLL
    {
        private readonly IConfiguration _configuration;
        private readonly IAutorizacionBLL _autorizacionBLL;
        private readonly IUtilidades _utilidades;
        private readonly IUsuarioDAL _usuarioDAL;
        private readonly IPlantillasCorreoService _plantillaCorreo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;

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

        #region Métodos PÚBLICOS
        //Realiza todas las validaciones para permitir el acceso (LogIn) del usuario al sistema
        public async Task<Respuesta<UsuarioResponseDTO>> AutenticarUsuario(UsuarioLoginRequestDTO dtoUsuario)
        {
            try
            {
                Respuesta<Usuario> resultOperacion = new Respuesta<Usuario>
                {
                    Objeto = await _usuarioDAL.ConsultarUsuarioPorId(dtoUsuario.Email)
                };

                if (resultOperacion.Objeto != null)
                {
                    bool contrasenaValidada = _utilidades.VerificarContrasena(dtoUsuario.Contrasena, resultOperacion.Objeto.ContrasenaHash);

                    if (!resultOperacion.Objeto.Confirmado && !resultOperacion.Objeto.Restablecer && !string.IsNullOrEmpty(resultOperacion.Objeto.ContrasenaHash))
                    {
                        return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = $"Falta por confirmar su cuenta. Se le envió un correo de solicitud de confirmación a '{dtoUsuario.Email}'." };
                    }
                    else if (resultOperacion.Objeto.Restablecer && !resultOperacion.Objeto.Confirmado && string.IsNullOrEmpty(resultOperacion.Objeto.ContrasenaHash))
                    {
                        return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = $"Se ha solicitado restablecer su cuenta. Favor revise la bandeja de su correo '{dtoUsuario.Email}'." };
                    }
                    else if (!contrasenaValidada)
                    {
                        return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "La contraseña no coincide con la que hay almacenada en el sistema." };
                    }
                    else
                    {
                        resultOperacion = await _autorizacionBLL.GenerarAccessTokenYRefreshTokenConCredenciales(dtoUsuario.Email);
                        return new Respuesta<UsuarioResponseDTO> { IsSuccess = true, Objeto = _mapper.Map<UsuarioResponseDTO>(resultOperacion.Objeto), Mensaje = "¡Autenticación exitosa!" };
                    }
                }
                else
                {
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "No se encontraron coincidencias con esas credenciales. Favor revisar los datos con los que está intentando acceder al sistema." };
                }
            }
            catch (Exception e)
            {
                return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        //Realiza todas las validaciones para permitir el registro (SignUp) del usuario en el sistema
        public async Task<Respuesta<UsuarioResponseDTO>> RegistrarUsuario(UsuarioRegistroRequestDTO dtoUsuario)
        {
            try
            {
                var existeUsuario = await ConsultarUsuarioPorId(dtoUsuario.Email);

                if (existeUsuario.IsSuccess)
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = $"El correo electrónico proporcionado ya se encuentra registrado en el sistema. Por favor acceda con sus credenciales de acceso." };

                if (string.IsNullOrEmpty(dtoUsuario.NombreApellido))
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "Campo de Nombre y Apellido es obligatorio." };

                if (!Regex.IsMatch(dtoUsuario.Email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "Formato de correo electrónico inválido." };

                if (dtoUsuario.Contrasena.Length < 12 || !Regex.IsMatch(dtoUsuario.Contrasena, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[^\s]+$"))
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "Formato de contraseña inválido. La contraseña debe contener 12 caracteres como mínimo, al menos una minúscula, una mayúscula, un número, un caracter especial y no debe contener espacios." };


                //Nuevo usuario
                Usuario usuario = _mapper.Map<Usuario>(dtoUsuario);
                usuario.ContrasenaHash = _utilidades.EncriptarContraseña(dtoUsuario.Contrasena);
                usuario.Restablecer = false;
                usuario.Confirmado = false;
                usuario.GuidAcceso = _utilidades.GenerarGuid();
                usuario.FechaCreacionGuid = _utilidades.FechaHoraActualColombia();
                usuario.FechaExpiracionGuid = usuario.FechaCreacionGuid.AddMinutes(_configuration.GetValue<int>("GuidAcceso_ExpirationTime"));
                usuario.GuidValidado = false;

                var respuesta = await _usuarioDAL.RegistrarUsuario(usuario);

                if (respuesta)
                {
                    PlantillaCorreo? plantillaCorreo = await ObtenerPlantillaPorEnum(PlantillasCorreoEnum.ConfirmarCorreo);

                    HttpRequest urlHost = _httpContextAccessor.HttpContext!.Request;
                    string url = $"{urlHost.Scheme}://{urlHost.Host}{urlHost.PathBase}{$"/api/Usuario/ConfirmarCuenta?guidAcceso={usuario.GuidAcceso}"}";

                    string htmlBody = string.Format(plantillaCorreo.Cuerpo, usuario.NombreApellido, url);

                    InfoCorreo infoCorreo = new InfoCorreo()
                    {
                        Asunto = plantillaCorreo.Asunto,
                        Para = usuario.Email,
                        Contenido = htmlBody
                    };

                    bool correoEnviado = _utilidades.EnviarCorreo(infoCorreo);

                    if (correoEnviado)
                        return new Respuesta<UsuarioResponseDTO> { IsSuccess = true, Mensaje = $"Su cuenta ha sido creada satisfactoriamente. Hemos enviado un mensaje al correo '{usuario.Email}' para confirmar su cuenta." };
                    else
                        return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = $"No fue posible enviar el correo a '{usuario.Email}'." };
                }
                else
                {
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = $"No se pudo crear su cuenta." };
                }
            }
            catch (Exception e)
            {
                return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        //Resetea la contraseña del usuario, para q posteriomente pueda restablecer su contraseña con 'ActualizarContrasenaAntigua()'
        public async Task<Respuesta<UsuarioResponseDTO>> OlvidoSuContrasena(string email)
        {
            try
            {
                var usuarioEncontrado = await ConsultarUsuarioPorId(email);
                if (usuarioEncontrado.IsSuccess)
                {
                    string newGuidAcceso = _utilidades.GenerarGuid();
                    DateTime fechaCreacionGuid = _utilidades.FechaHoraActualColombia();
                    DateTime fechaExpiracionGuid = _utilidades.FechaHoraActualColombia().AddMinutes(_configuration.GetValue<int>("GuidAcceso_ExpirationTime"));

                    Usuario usuarioRestablecido = new Usuario
                    {
                        IdUsuario = usuarioEncontrado.Objeto.IdUsuario,
                        GuidAcceso = newGuidAcceso,
                        FechaCreacionGuid = fechaCreacionGuid,
                        FechaExpiracionGuid = fechaExpiracionGuid,
                        ContrasenaHash = string.Empty,
                        Restablecer = true,
                        Confirmado = true,
                        GuidValidado = false
                    };

                    bool respuesta = await _usuarioDAL.RestablecerContrasena(usuarioRestablecido);
                    if (respuesta)
                    {
                        PlantillaCorreo? plantillaCorreo = await ObtenerPlantillaPorEnum(PlantillasCorreoEnum.RestablecerContrasena);

                        HttpRequest urlHost = _httpContextAccessor.HttpContext!.Request;
                        string url = $"{_configuration.GetValue<string>("Frontend_URLs:Desarrollo")}/{$"password?guidAcceso={newGuidAcceso}"}"; //desde Frontend
                        //string url = $"{urlHost.Scheme}://{urlHost.Host}{urlHost.PathBase}{$"/api/Usuario/RestablecerContrasena?guidAcceso={newGuidAcceso}"}"; //desde Backend

                        string htmlBody = string.Format(plantillaCorreo.Cuerpo, usuarioEncontrado.Objeto.NombreApellido, url);

                        InfoCorreo correoDTO = new InfoCorreo()
                        {
                            Asunto = plantillaCorreo.Asunto,
                            Para = usuarioEncontrado.Objeto.Email,
                            Contenido = htmlBody
                        };

                        bool correoEnviado = _utilidades.EnviarCorreo(correoDTO);

                        if (correoEnviado)
                            return new Respuesta<UsuarioResponseDTO> { IsSuccess = true, Mensaje = "La solicitud de reestablecimiento de contraseña fue procesada satisfactoriamente. Por favor revise la bandeja de entrada de su correo electrónico para actualizar su contraseña." };
                        else
                            return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = $"¡ERROR! No fue posible procesar su solicitud de envío de correo para el cambio de contraseña." };
                    }
                    else
                    {
                        return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = $"¡ERROR! No se pudo restablecer su contraseña." };
                    }
                }
                else
                {
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = $"No se encontraron coincidencias con el correo proporcionado." };
                }
            }
            catch (Exception e)
            {
                return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        //Actualiza la contraseña antigua (reseteada con 'OlvidoSuContrasena()') del usuario
        public async Task<Respuesta<UsuarioResponseDTO>> ActualizarContrasenaAntigua(string guidAcceso, string nuevaContrasena, string confirmacionContrasena)
        {
            try
            {
                if (nuevaContrasena != confirmacionContrasena)
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "Las contraseñas ingresadas no coinciden." };

                if (nuevaContrasena.Length < 12 || !Regex.IsMatch(nuevaContrasena, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[^\s]+$"))
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "Formato de contraseña inválido. La contraseña debe contener 12 caracteres como mínimo, al menos una minúscula, una mayúscula, un número, un caracter especial y no debe contener espacios." };

                var existeGuid = await ConsultarUsuarioPorGuid(guidAcceso);
                if (!existeGuid.IsSuccess)
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "Solicitud no existe o ya se encuentra inválida." };
                else if (existeGuid.IsSuccess && (existeGuid.Objeto.GuidValidado || !existeGuid.Objeto.GuidActivo))
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "¡El enlace por el cual solicitaste el cambio de contraseña ya se encuentra inválido, ha expirado, o ya habías realizado un cambio de contraseña anteriormente usando este correo!" };

                string contrasenaHash = _utilidades.EncriptarContraseña(nuevaContrasena);
                bool respuesta = await _usuarioDAL.ActualizarContrasenaAntigua(guidAcceso, contrasenaHash);

                if (respuesta)
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = true, Mensaje = "¡Contraseña actualizada satisfactoriamente!" };
                else
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = "¡ERROR! No se pudo actualizar la contraseña. Favor usar el correo con la última solicitud de cambio de contraseña generada." };
            }
            catch (Exception e)
            {
                return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        //Confirma la cuenta del usuario luego de haber recibido el correo de Bienvenida para que ya el sistema le permita loguearse en la aplicación
        public async Task<Respuesta<UsuarioResponseDTO>> ConfirmarCuenta(string guidAcceso)
        {
            try
            {
                bool respuesta = false;
                var existeGuid = await ConsultarUsuarioPorGuid(guidAcceso);

                if (existeGuid.IsSuccess && !existeGuid.Objeto.Confirmado)
                    respuesta = await _usuarioDAL.ConfirmarCuenta(guidAcceso);
                else
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = existeGuid.Mensaje };

                if (respuesta)
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = true, Mensaje = "¡Confirmación de cuenta realizada satisfactoriamente!" };
                else
                    return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = $"¡ERROR! No se pudo confirmar la cuenta. {existeGuid.Mensaje}" };
            }
            catch (Exception e)
            {
                return new Respuesta<UsuarioResponseDTO> { IsSuccess = false, Mensaje = e.Message };
            }
        }
        #endregion Métodos PÚBLICOS

        #region Métodos PRIVADOS
        //Consulta a un usuario por el GUID enviado dentro del link de un correo
        private async Task<Respuesta<Usuario>> ConsultarUsuarioPorGuid(string guidUsuario)
        {
            try
            {
                Respuesta<Usuario> resultOperacion = new Respuesta<Usuario>
                {
                    Objeto = await _usuarioDAL.ConsultarUsuarioPorGuid(guidUsuario)
                };

                if (resultOperacion.Objeto == null || !resultOperacion.Objeto.GuidActivo)
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "GUID no existe o ya se encuentra inválido. Favor solicite el reestablecimiento de su contraseña." };
                else
                    return new Respuesta<Usuario> { IsSuccess = true, Objeto = resultOperacion.Objeto, Mensaje = "¡GUID existe en la BD!" };
            }
            catch (Exception e)
            {
                return new Respuesta<Usuario> { IsSuccess = false, Mensaje = e.Message };
            }
        }

        //Consulta a un usuario por su Id (PK identificador de BD)
        private async Task<Respuesta<Usuario>> ConsultarUsuarioPorId(string email)
        {
            try
            {
                Respuesta<Usuario> resultOperacion = new Respuesta<Usuario>
                {
                    Objeto = await _usuarioDAL.ConsultarUsuarioPorId(email)
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

        //Retorna la plantilla del correo solicitada, ya sea la de 'RegistrarUsuario' o la de 'OlvidoSuContrasena'
        private async Task<PlantillaCorreo> ObtenerPlantillaPorEnum(PlantillasCorreoEnum tipoPlantilla)
        {
            List<PlantillaCorreo> plantillas = await _plantillaCorreo.CargarPlantillasCorreoDesdeDB();
            PlantillaCorreo plantilla = plantillas.FirstOrDefault(p => p.Nombre == tipoPlantilla.ToString())!;
            return plantilla;
        }
        #endregion Métodos PRIVADOS

    }
}
