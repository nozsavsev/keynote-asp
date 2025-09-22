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
        public ActionResult<ResponseWrapper<KeynoteDtosResponse>> GetKeynoteDtos()
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
        public ActionResult<ResponseWrapper<EnumsResponse>> GetEnums()
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
        public ActionResult<ResponseWrapper<ListDtosResponse>> GetListDtos()
        {
            var response = new ListDtosResponse
            {
                RoomList = new List<TR_RoomDTO>(),
                PresentorList = new List<TR_PresentorDTO>(),
                ScreenList = new List<TR_ScreenDTO>(),
                SpectatorList = new List<TR_SpectatorDTO>(),
                KeynoteList = new List<KeynoteDTO>(),
                UserList = new List<UserDTO>(),
                SessionList = new List<SessionDTO>(),
                ServiceList = new List<ServiceDTO>(),
                PermissionList = new List<PermissionDTO>(),
                EmailTemplateList = new List<EmailTemplateDTO>(),
                EmailActionList = new List<EmailActionDTO>()
            };

            return Ok(new ResponseWrapper<ListDtosResponse>(WrResponseStatus.Ok, response));
        }

        /// <summary>
        /// Returns session identifiers for cookie-based authentication
        /// </summary>
        [HttpGet("session-identifiers")]
        public ActionResult<ResponseWrapper<SessionIdentifiersResponse>> GetSessionIdentifiers()
        {
            var response = new SessionIdentifiersResponse
            {
                ScreenIdentifier = "screen_123456",
                SpectatorIdentifier = "spectator_123456",
                PresentorSessionId = "nauth_session_123456"
            };

            return Ok(new ResponseWrapper<SessionIdentifiersResponse>(WrResponseStatus.Ok, response));
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
        public List<UserDTO> UserList { get; set; } = new();
        public List<SessionDTO> SessionList { get; set; } = new();
        public List<ServiceDTO> ServiceList { get; set; } = new();
        public List<PermissionDTO> PermissionList { get; set; } = new();
        public List<EmailTemplateDTO> EmailTemplateList { get; set; } = new();
        public List<EmailActionDTO> EmailActionList { get; set; } = new();
    }

    public class SessionIdentifiersResponse
    {
        public string ScreenIdentifier { get; set; } = string.Empty;
        public string SpectatorIdentifier { get; set; } = string.Empty;
        public string PresentorSessionId { get; set; } = string.Empty;
    }

    #endregion
}
