
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
    public class KeynoteController(KeynoteService keynoteService, IMapper mapper, ILogger<UserController> logger) : ControllerBase
    {

        [HttpPost]
        [Route("createKeynote")]
        [Authorize]
        public async Task<ActionResult<ResponseWrapper<KeynoteDTO>>> Create(CreateKeynoteDTO keynote)
        {
            try
            {
                var KeynoteUser = HttpContext.GetKeynoteUser();
                var NauthSession = HttpContext.GetNauthSession();

                var newKeynote = new DB_Keynote();

                var db_keynote = mapper.Map(keynote, newKeynote);

                db_keynote.UserId = KeynoteUser!.Id;

                var created = await keynoteService.AddAsync(db_keynote);

                return Ok(new ResponseWrapper<KeynoteDTO>(WrResponseStatus.Ok, mapper.Map<KeynoteDTO>(created)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while creating keynote");
                return StatusCode(500, new ResponseWrapper<KeynoteDTO>(WrResponseStatus.InternalError, null));
            }
        }

        [HttpDelete]
        [Route("deleteKeynote")]
        [Authorize]
        public async Task<ActionResult<ResponseWrapper<string>>> Delete(string keynoteId)
        {
            try
            {
                var KeynoteUser = HttpContext.GetKeynoteUser();
                var NauthSession = HttpContext.GetNauthSession();

                var newKeynote = new DB_Keynote();

                await keynoteService.DeleteByidAsync(long.Parse(keynoteId));

                return Ok(new ResponseWrapper<string>(WrResponseStatus.Ok));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while deleting keynote");
                return StatusCode(500, new ResponseWrapper<string>(WrResponseStatus.InternalError, null));
            }
        }
    }
}
