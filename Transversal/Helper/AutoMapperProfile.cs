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
            //Usuario
            CreateMap<RegistroUsuarioDTO, Usuario>().ReverseMap();
        }

    }
}
