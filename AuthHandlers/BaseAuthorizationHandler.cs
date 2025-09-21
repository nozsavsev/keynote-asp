using Microsoft.AspNetCore.Authorization;
using keynote_asp.AuthHandlers.AuthorizationRequirments;
using System.Security.Claims;
using Keynote_asp.Nauth.API_GEN.Models;

namespace keynote_asp.AuthHandlers
{
    public abstract class BaseAuthorizationHandler<TRequirement> : AuthorizationHandler<TRequirement>
        where TRequirement : BaseAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TRequirement requirement)
        {
            if (context.User == null || !context.User.Identity.IsAuthenticated)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var httpContext = context.Resource as HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var nauthUser = httpContext.GetNauthUser();
            var nauthSession = httpContext.GetNauthSession();

            if (nauthUser == null || nauthSession == null)
            {
                httpContext.AddAuthenticationFailureReason(AuthFailureReasons.SessionExpired);
                context.Fail();
                return Task.CompletedTask;
            }

            // Check email verification
            if (requirement.RequireEmailVerified && !(nauthUser.IsEmailVerified ?? false))
            {
                httpContext.AddAuthenticationFailureReason(AuthFailureReasons.RequireVerifiedEmail);
                context.Fail();
                return Task.CompletedTask;
            }

            // Check if user is enabled
            if (requirement.RequireEnabled && !(nauthUser.IsEnabled ?? false))
            {
                httpContext.AddAuthenticationFailureReason(AuthFailureReasons.RequireEnabledUser);
                context.Fail();
                return Task.CompletedTask;
            }

            // Check 2FA confirmation
            if (requirement.Require2FAConfirmed && !(nauthSession.Is2FAConfirmed ?? false))
            {
                httpContext.AddAuthenticationFailureReason(AuthFailureReasons._2FARequired);
                context.Fail();
                return Task.CompletedTask;
            }

            return HandleAdditionalRequirementsAsync(context, requirement, httpContext);
        }

        protected abstract Task HandleAdditionalRequirementsAsync(
            AuthorizationHandlerContext context,
            TRequirement requirement,
            HttpContext httpContext);
    }
}
