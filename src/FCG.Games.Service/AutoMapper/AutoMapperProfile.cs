
using AutoMapper;
using FCG.Games.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FCG.Games.Service.AutoMapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<IdentityUser, DTO.Response.RegisterUserResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ReverseMap();

            CreateMap<Game, DTO.Response.GameResponseDto>()
                .ReverseMap();

            CreateMap<Game, DTO.Request.GameRequestDto>()
                .ReverseMap();

            




        }
    }   
    
}
