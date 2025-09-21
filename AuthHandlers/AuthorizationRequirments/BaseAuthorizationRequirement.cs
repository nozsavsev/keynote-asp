using Microsoft.AspNetCore.Authorization;

namespace keynote_asp.AuthHandlers.AuthorizationRequirments
{
    public class BaseAuthorizationRequirement : IAuthorizationRequirement
    {
        public bool RequireEmailVerified { get; set; } = true;
        public bool RequireEnabled { get; set; } = true;
        public bool Require2FAConfirmed { get; set; } = true;

        public BaseAuthorizationRequirement(
            bool requireEmailVerified = true,
            bool requireEnabled = true,
            bool require2FAConfirmed = true)
        {
            RequireEmailVerified = requireEmailVerified;
            RequireEnabled = requireEnabled;
            Require2FAConfirmed = require2FAConfirmed;
        }
    }
}
