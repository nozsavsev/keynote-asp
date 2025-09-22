using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using keynote_asp.Services.Transient;
using keynote_asp.Models.Transient;

namespace keynote_asp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class SessionController : ControllerBase
    {
        [HttpGet]
        [Route("GetScreenSession")]
        public ActionResult<string> GetScreenSession()
        {
            try
            {
                // Create a new screen object
                var screen = new TR_Screen();
                ScreenService.AddOrUpdate(screen);

                // Set the screen identifier as a cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(24) // 24 hour expiry
                };

                Response.Cookies.Append("ScreenIdentifier", screen.Identifier, cookieOptions);

                return Ok(screen.Identifier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating screen session: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetSpectatorSession")]
        public ActionResult<string> GetSpectatorSession()
        {
            try
            {
                // Create a new spectator object
                var spectator = new TR_Spectator();
                SpectatorService.AddOrUpdate(spectator);

                // Set the spectator identifier as a cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(24) // 24 hour expiry
                };

                Response.Cookies.Append("SpectatorIdentifier", spectator.Identifier, cookieOptions);

                return Ok(spectator.Identifier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating spectator session: {ex.Message}");
            }
        }
    }
}
