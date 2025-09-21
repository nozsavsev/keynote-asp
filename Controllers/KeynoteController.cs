using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using keynote_asp.Services;
using keynote_asp.Models.Keynote;
using keynote_asp.Dtos;
using keynote_asp.Exceptions;

namespace keynote_asp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Default policy - requires authenticated, email verified, enabled, 2FA confirmed
    public class KeynoteController : ControllerBase
    {
        private readonly IKeynoteService _keynoteService;

        public KeynoteController(IKeynoteService keynoteService)
        {
            _keynoteService = keynoteService;
        }

        // Get all keynotes for the current user
        [HttpGet]
        public async Task<IActionResult> GetMyKeynotes()
        {
            var user = HttpContext.GetKeynoteUser();
            if (user == null)
            {
                throw new KeynoteException(WrResponseStatus.Unauthorized);
            }

            // TODO: Implement get keynotes by user
            return Ok(new ResponseWrapper<List<DB_Keynote>>(WrResponseStatus.Success, new List<DB_Keynote>()));
        }

        // Get a specific keynote - requires user owns the keynote
        [HttpGet("{id}")]
        [Authorize(Policy = "UserOwnsKeynote")]
        public async Task<IActionResult> GetKeynote(int id)
        {
            var keynote = await _keynoteService.GetByIdAsync(id);
            if (keynote == null)
            {
                throw new KeynoteException(WrResponseStatus.NotFound);
            }

            return Ok(new ResponseWrapper<DB_Keynote>(WrResponseStatus.Success, keynote));
        }

        // Create a new keynote - no email verification required
        [HttpPost]
        [Authorize(Policy = "AllowNoVerifiedEmail")]
        public async Task<IActionResult> CreateKeynote([FromBody] CreateKeynoteDto dto)
        {
            var user = HttpContext.GetKeynoteUser();
            if (user == null)
            {
                throw new KeynoteException(WrResponseStatus.Unauthorized);
            }

            // TODO: Implement create keynote
            return Ok(new ResponseWrapper<DB_Keynote>(WrResponseStatus.Success, null));
        }

        // Update a keynote - requires user owns the keynote
        [HttpPut("{id}")]
        [Authorize(Policy = "UserOwnsKeynote")]
        public async Task<IActionResult> UpdateKeynote(int id, [FromBody] UpdateKeynoteDto dto)
        {
            // TODO: Implement update keynote
            return Ok(new ResponseWrapper<DB_Keynote>(WrResponseStatus.Success, null));
        }

        // Delete a keynote - requires user owns the keynote
        [HttpDelete("{id}")]
        [Authorize(Policy = "UserOwnsKeynote")]
        public async Task<IActionResult> DeleteKeynote(int id)
        {
            await _keynoteService.DeleteByidAsync(id);
            return Ok(new ResponseWrapper<string>(WrResponseStatus.Success, "Keynote deleted successfully"));
        }

        // Admin endpoint - requires manage keynotes permission
        [HttpGet("admin/all")]
        [Authorize(Policy = "PrManageKeynotes")]
        public async Task<IActionResult> GetAllKeynotes()
        {
            // TODO: Implement get all keynotes for admin
            return Ok(new ResponseWrapper<List<DB_Keynote>>(WrResponseStatus.Success, new List<DB_Keynote>()));
        }
    }

    // DTOs
    public class CreateKeynoteDto
    {
        public required string Name { get; set; }
    }

    public class UpdateKeynoteDto
    {
        public required string Name { get; set; }
    }
}
