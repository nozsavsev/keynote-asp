using Microsoft.AspNetCore.Authorization;
using keynote_asp.AuthHandlers.AuthorizationRequirments;

namespace keynote_asp.AuthHandlers
{
    public class BaseRequirementHandler : BaseAuthorizationHandler<BaseAuthorizationRequirement>
    {
        public BaseRequirementHandler(ILogger<BaseRequirementHandler> logger) : base(logger)
        {
        }

        protected override Task HandleAdditionalRequirementsAsync(
            AuthorizationHandlerContext context,
            BaseAuthorizationRequirement requirement,
            HttpContext httpContext)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
