using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transversal.DTOs;
using Transversal.Model;

namespace Transversal.Helper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            #region Usuario
            CreateMap<Usuario, UsuarioRegistroRequestDTO>().ReverseMap();

            CreateMap<Usuario, UsuarioLoginRequestDTO>().ReverseMap();
            
            CreateMap<Usuario, UsuarioResponseDTO>().ReverseMap();
            #endregion Usuario
        }

    }
}
