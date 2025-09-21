using Microsoft.AspNetCore.Authorization;
using keynote_asp.AuthHandlers.AuthorizationRequirments;
using keynote_asp.Repositories;

namespace keynote_asp.AuthHandlers
{
    public class UserOwnsKeynoteHandler : AuthorizationHandler<UserOwnsKeynoteRequirement>
    {
        private readonly KeynoteRepository _keynoteRepository;

        public UserOwnsKeynoteHandler(KeynoteRepository keynoteRepository)
        {
            _keynoteRepository = keynoteRepository;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            UserOwnsKeynoteRequirement requirement)
        {
            var httpContext = context.Resource as HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return;
            }

            var keynoteUser = httpContext.GetKeynoteUser();
            if (keynoteUser == null)
            {
                context.Fail();
                return;
            }

            // Get keynote ID from route
            if (httpContext.Request.RouteValues.TryGetValue("id", out var keynoteIdObj) && 
                int.TryParse(keynoteIdObj?.ToString(), out var keynoteId))
            {
                var keynote = await _keynoteRepository.GetByIdAsync(keynoteId);
                
                if (keynote != null && keynote.UserId == keynoteUser.Id)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            context.Fail();
        }
    }
}
