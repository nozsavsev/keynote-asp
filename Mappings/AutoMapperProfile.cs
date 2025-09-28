using AutoMapper;
using keynote_asp.Dtos;
using keynote_asp.Models.Keynote;
using keynote_asp.Models.Transient;
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



            CreateMap<CreateKeynoteDTO, DB_Keynote>();
            
            CreateMap<KeynoteDTO, DB_Keynote>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));

            CreateMap<DB_Keynote, KeynoteDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()));

            CreateMap<TR_BaseEntity, TR_BaseEntityDTO>();

            CreateMap<TR_Room, TR_RoomDTO>()
                .ForMember(dest => dest.Presentor, opt => opt.MapFrom(src => src.Presentor))
                .ForMember(dest => dest.Screen, opt => opt.MapFrom(src => src.Screen))
                .ForMember(dest => dest.Spectators, opt => opt.MapFrom(src => src.Spectators));
            
            CreateMap<TR_Presentor, TR_PresentorDTO>();
            CreateMap<TR_Screen, TR_ScreenDTO>();
            CreateMap<TR_Spectator, TR_SpectatorDTO>();
        }
    }
}
