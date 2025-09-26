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
                Request.Cookies.TryGetValue("ScreenIdentifier", out var identity);
                var screen = ScreenService.GetOrCreate(identity!);

                // Set the screen identifier as a cookie
                // Replace this in both GetScreenSession and GetSpectatorSession:
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax, // Changed from Strict to Lax
                    Expires = DateTimeOffset.UtcNow.AddHours(24),
                    Path = "/" // Explicitly set path
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
                Request.Cookies.TryGetValue("SpectatorIdentifier", out var identity);
                var spectator = SpectatorService.GetOrCreate(identity!);

                // Set the spectator identifier as a cookie
                // Replace this in both GetScreenSession and GetSpectatorSession:
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax, // Changed from Strict to Lax
                    Expires = DateTimeOffset.UtcNow.AddHours(24),
                    Path = "/" // Explicitly set path
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
