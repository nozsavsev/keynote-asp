using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using keynote_asp.Services;
using keynote_asp.Models.Keynote;
using keynote_asp.Dtos;
using keynote_asp.Exceptions;
using AutoMapper;
using keynote_asp.Models.User;

namespace keynote_asp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserController> _logger;

        public UserController(UserService userService, IMapper mapper, ILogger<UserController> logger)
        {
            _userService = userService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [Route("currentUser")]
        [Authorize]
        public async Task<ActionResult<ResponseWrapper<MeDTO>>> GetCurrentUser()
        {
            try
            {
                var KeynoteUser = HttpContext.GetKeynoteUser();
                var NauthSession = HttpContext.GetNauthSession();

                if (KeynoteUser == null)
                {
                    return StatusCode(500, new ResponseWrapper<MeDTO>(WrResponseStatus.InternalError, null));
                }

                if (NauthSession?.User == null)
                {
                    return StatusCode(500, new ResponseWrapper<MeDTO>(WrResponseStatus.InternalError, null));
                }

                var dto = new MeDTO
                {
                    KeynoteUser = _mapper.Map<KeynoteUserDTO>(KeynoteUser),
                    NauthUser = _mapper.Map<Keynote_asp.Nauth.API_GEN.Models.UserDTO>(NauthSession.User)
                };

                return Ok(new ResponseWrapper<MeDTO>(WrResponseStatus.Ok, dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting current user");
                return StatusCode(500, new ResponseWrapper<MeDTO>(WrResponseStatus.InternalError, null));
            }
        }

        [HttpGet]
        [Route("currentUserManage")]
        [Authorize("PrAdminManageKeynotes")]
        public async Task<ActionResult<ResponseWrapper<MeDTO>>> GetCurrentUserD()
        {
            try
            {
                var KeynoteUser = HttpContext.GetKeynoteUser();
                var NauthSession = HttpContext.GetNauthSession();

                if (KeynoteUser == null)
                {
                    return StatusCode(500, new ResponseWrapper<MeDTO>(WrResponseStatus.InternalError, null));
                }

                if (NauthSession?.User == null)
                {
                    return StatusCode(500, new ResponseWrapper<MeDTO>(WrResponseStatus.InternalError, null));
                }

                var dto = new MeDTO
                {
                    KeynoteUser = _mapper.Map<KeynoteUserDTO>(KeynoteUser),
                    NauthUser = _mapper.Map<Keynote_asp.Nauth.API_GEN.Models.UserDTO>(NauthSession.User)
                };

                return Ok(new ResponseWrapper<MeDTO>(WrResponseStatus.Ok, dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting current user");
                return StatusCode(500, new ResponseWrapper<MeDTO>(WrResponseStatus.InternalError, null));
            }
        }

        [HttpGet]
        [Route("currentUserUpload")]
        [Authorize("PrUploadFiles")]
        public async Task<ActionResult<ResponseWrapper<MeDTO>>> GetCurrentUserE()
        {
            try
            {
                var KeynoteUser = HttpContext.GetKeynoteUser();
                var NauthSession = HttpContext.GetNauthSession();

                if (KeynoteUser == null)
                {
                    return StatusCode(500, new ResponseWrapper<MeDTO>(WrResponseStatus.InternalError, null));
                }

                if (NauthSession?.User == null)
                {
                    return StatusCode(500, new ResponseWrapper<MeDTO>(WrResponseStatus.InternalError, null));
                }

                var dto = new MeDTO
                {
                    KeynoteUser = _mapper.Map<KeynoteUserDTO>(KeynoteUser),
                    NauthUser = _mapper.Map<Keynote_asp.Nauth.API_GEN.Models.UserDTO>(NauthSession.User)
                };

                return Ok(new ResponseWrapper<MeDTO>(WrResponseStatus.Ok, dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting current user");
                return StatusCode(500, new ResponseWrapper<MeDTO>(WrResponseStatus.InternalError, null));
            }
        }

    }
}
