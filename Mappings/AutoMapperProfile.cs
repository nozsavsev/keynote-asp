
using AutoMapper;
using keynote_asp.Dtos;
using keynote_asp.Models.Keynote;
using keynote_asp.Models.User;

namespace keynote_asp.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Nauth mappings
            CreateMap<DB_User, Keynote_asp.Nauth.API_GEN.Models.UserDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));

            // Keynote DTO mappings
            CreateMap<DB_User, KeynoteUserDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.Keynotes, opt => opt.MapFrom(src => src.keynotes));

            CreateMap<DB_Keynote, KeynoteDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));
        }
    }
}
