using Microsoft.AspNetCore.Mvc;
using keynote_asp.Models.Transient;
using keynote_asp.Dtos;
using keynote_asp.Models.Keynote;
using Keynote_asp.Nauth.API_GEN.Models;

namespace keynote_asp.Controllers
{
    /// <summary>
    /// This controller exists solely for Swagger documentation purposes.
    /// It exposes all DTOs used in the application so the frontend can generate proper TypeScript types.
    /// These endpoints should never be called in production.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)] // Ensure Swagger picks this up
    public class SwaggerDtoController : ControllerBase
    {
        /// <summary>
        /// Returns all transient room-related DTOs for Swagger documentation
        /// </summary>
        [HttpGet("room-dtos")]
        public ActionResult<ResponseWrapper<RoomDtosResponse>> GetRoomDtos()
        {
            var response = new RoomDtosResponse
            {
                Room = new TR_RoomDTO(),
                Presentor = new TR_PresentorDTO(),
                Screen = new TR_ScreenDTO(),
                Spectator = new TR_SpectatorDTO(),
                BaseEntity = new TR_BaseEntityDTO()
            };

            return Ok(new ResponseWrapper<RoomDtosResponse>(WrResponseStatus.Ok, response));
        }

        /// <summary>
        /// Returns all keynote-related DTOs for Swagger documentation
        /// </summary>
        [HttpGet("keynote-dtos")]
        public ActionResult<ResponseWrapper<KeynoteDtosResponse>> GetKeynoteDtos(KeynoteDtosResponse dtos)
        {
            var response = new KeynoteDtosResponse
            {
                Keynote = new KeynoteDTO(),
                CreateKeynote = new CreateKeynoteDTO(),
                KeynoteUser = new KeynoteUserDTO(),
            };

            return Ok(new ResponseWrapper<KeynoteDtosResponse>(WrResponseStatus.Ok, response));
        }


        /// <summary>
        /// Returns all enum types used in the application
        /// </summary>
        [HttpGet("enums")]
        public ActionResult<ResponseWrapper<EnumsResponse>> GetEnums(EnumsResponse enums)
        {
            var response = new EnumsResponse
            {
                AuthFailureReasons = AuthFailureReasons.SessionExpired,
                WrResponseStatus = WrResponseStatus.Ok,
                KeynoteTransitionType = KeynoteTransitionType.none,
                KeynoteType = KeynoteType.Pdf
            };

            return Ok(new ResponseWrapper<EnumsResponse>(WrResponseStatus.Ok, response));
        }

        /// <summary>
        /// Returns all list/array DTOs for Swagger documentation
        /// </summary>
        [HttpGet("list-dtos")]
        public ActionResult<ResponseWrapper<ListDtosResponse>> GetListDtos(ListDtosResponse lists)
        {
            var response = new ListDtosResponse
            {
                RoomList = new List<TR_RoomDTO>(),
                PresentorList = new List<TR_PresentorDTO>(),
                ScreenList = new List<TR_ScreenDTO>(),
                SpectatorList = new List<TR_SpectatorDTO>(),
                KeynoteList = new List<KeynoteDTO>(),
            };

            return Ok(new ResponseWrapper<ListDtosResponse>(WrResponseStatus.Ok, response));
        }
    }

    #region Response DTOs for Swagger

    public class RoomDtosResponse
    {
        public TR_RoomDTO Room { get; set; } = new();
        public TR_PresentorDTO Presentor { get; set; } = new();
        public TR_ScreenDTO Screen { get; set; } = new();
        public TR_SpectatorDTO Spectator { get; set; } = new();
        public TR_BaseEntityDTO BaseEntity { get; set; } = new();
    }

    public class KeynoteDtosResponse
    {
        public KeynoteDTO Keynote { get; set; } = new();
        public CreateKeynoteDTO CreateKeynote { get; set; } = new();
        public KeynoteUserDTO KeynoteUser { get; set; } = new();
    }

    public class EnumsResponse
    {
        public AuthFailureReasons AuthFailureReasons { get; set; }
        public WrResponseStatus WrResponseStatus { get; set; }
        public KeynoteTransitionType KeynoteTransitionType { get; set; }
        public KeynoteType KeynoteType { get; set; }
    }

    public class ListDtosResponse
    {
        public List<TR_RoomDTO> RoomList { get; set; } = new();
        public List<TR_PresentorDTO> PresentorList { get; set; } = new();
        public List<TR_ScreenDTO> ScreenList { get; set; } = new();
        public List<TR_SpectatorDTO> SpectatorList { get; set; } = new();
        public List<KeynoteDTO> KeynoteList { get; set; } = new();
    }


    #endregion
}
