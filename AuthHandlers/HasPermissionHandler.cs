using Microsoft.AspNetCore.Authorization;
using keynote_asp.AuthHandlers.AuthorizationRequirments;

namespace keynote_asp.AuthHandlers
{
    public class HasPermissionHandler : BaseAuthorizationHandler<HasPermissionRequirement>
    {
        protected override Task HandleAdditionalRequirementsAsync(
            AuthorizationHandlerContext context,
            HasPermissionRequirement requirement,
            HttpContext httpContext)
        {
            var nauthUser = httpContext.GetNauthUser();
            
            if (nauthUser?.Permissions != null && 
                nauthUser.Permissions.Any(p => p.Permission?.Key == requirement.Permission))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
