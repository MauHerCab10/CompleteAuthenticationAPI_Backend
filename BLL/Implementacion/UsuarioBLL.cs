using AutoMapper;
using BLL.Interfaz;
using DAL.Implementacion;
using DAL.Interfaz;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        private readonly IAutorizacionBLL _autorizacionBLL;
        private readonly IUtilidades _utilidades;
        private readonly IUsuarioDAL _usuarioDAL;
        private readonly IPlantillasCorreoService _plantillaCorreo;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuarioBLL(IAutorizacionBLL autorizacionBLL, IUtilidades utilidades, IUsuarioDAL usuarioDAL, IPlantillasCorreoService plantillaCorreo, IMapper mapper, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _autorizacionBLL = autorizacionBLL;
            _utilidades = utilidades;
            _usuarioDAL = usuarioDAL;
            _plantillaCorreo = plantillaCorreo;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Respuesta<Usuario>> ConsultarUsuario(string email, string? contrasena = null)
        {
            try
            {
                Respuesta<Usuario> resultOperacion = new Respuesta<Usuario>
                {
                    Objeto = await _usuarioDAL.ConsultarUsuario(email, _utilidades.EncriptarContraseña(contrasena))
                };

                if (resultOperacion.Objeto == null)
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "Usuario no encontrado. Favor validar los datos ingresados." };
                else
                    return new Respuesta<Usuario> { IsSuccess = true, Objeto = resultOperacion.Objeto, Mensaje = "¡Autenticación exitosa!" };
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
                    Objeto = await _usuarioDAL.ConsultarUsuario(pUsuario.Email, _utilidades.EncriptarContraseña(pUsuario.Contrasena))
                };

                if (resultOperacion.Objeto != null)
                {
                    if (!resultOperacion.Objeto.Confirmado)
                    {
                        return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"Falta confirmar su cuenta. Se le envió un correo a {pUsuario.Email}." };
                    }
                    else if (resultOperacion.Objeto.Restablecer)
                    {
                        return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"Se ha solicitado restablecer su cuenta. Favor revise la bandeja de su correo {pUsuario.Email}." };
                    }
                    else
                    {
                        resultOperacion = await _autorizacionBLL.GenerarAccessTokenYRefreshTokenConCredenciales(new() { Email = pUsuario.Email, Contrasena = pUsuario.Contrasena }); //aqui debo encargarme de enviar un objeto tipo LoginUsuarioDTO
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
                var existeUsuario = await ConsultarUsuario(pUsuario.Email, _utilidades.EncriptarContraseña(pUsuario.Contrasena));

                if (existeUsuario.IsSuccess)
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"El correo electrónico proporcionado ya se encuentra registrado en el sistema." };

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
                    GuidAcceso = pUsuario.GuidAcceso
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
                        return new Respuesta<Usuario> { IsSuccess = true, Mensaje = $"Su cuenta ha sido creada satisfactoriamente. Hemos enviado un mensaje al correo {pUsuario.Email} para confirmar su cuenta." };
                    else
                        return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"¡ERROR! No fue posible enviar el correo a {pUsuario.Email}" };
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

        public async Task<Respuesta<Usuario>> ReestablecerContrasena(string email)
        {
            try
            {
                var usuarioEncontrado = await ConsultarUsuario(email);
                if (usuarioEncontrado.IsSuccess)
                {
                    bool respuesta = await _usuarioDAL.ReestablecerContrasena(1, 0, _utilidades.EncriptarContraseña(usuarioEncontrado.Objeto.Contrasena), usuarioEncontrado.Objeto.GuidAcceso);
                    if (respuesta)
                    {
                        HttpRequest urlHost = _httpContextAccessor.HttpContext!.Request;
                        string path = Path.Combine(_webHostEnvironment.ContentRootPath, "PlantillasCorreo", "RestablecerContrasena.html");
                        string content = System.IO.File.ReadAllText(path);
                        string url = $"{urlHost.Scheme}://{urlHost.Host}{urlHost.PathBase}{$"/api/Usuario/ActualizarContrasenaAntigua?guidAcceso={usuarioEncontrado.Objeto.GuidAcceso}"}";

                        string htmlBody = string.Format(content, usuarioEncontrado.Objeto.NombreApellido, url);

                        InfoCorreo correoDTO = new InfoCorreo()
                        {
                            Para = usuarioEncontrado.Objeto.Email,
                            Asunto = "Restablecer contraseña",
                            Contenido = htmlBody
                        };

                        bool correoEnviado = _utilidades.EnviarCorreo(correoDTO);

                        if (correoEnviado)
                            return new Respuesta<Usuario> { IsSuccess = true, Mensaje = "Se ha restablecido su contraseña satisfactoriamente." };
                        else
                            return new Respuesta<Usuario> { IsSuccess = false, Mensaje = $"¡ERROR! No fue posible reestablecer su contraseña." };
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
                {
                    return new Respuesta<Usuario> { IsSuccess = false, Mensaje = "Las contraseñas ingresadas no coinciden." };
                }

                string contrasenaEncriptada = _utilidades.EncriptarContraseña(nuevaContrasena);
                bool respuesta = await _usuarioDAL.ReestablecerContrasena(0, 1, contrasenaEncriptada, guidAcceso);

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

        public async Task<bool> ConfirmarCuenta(string guidAcceso)
        {
            bool respuesta = await _usuarioDAL.ConfirmarCuenta(guidAcceso);
            return respuesta;
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
