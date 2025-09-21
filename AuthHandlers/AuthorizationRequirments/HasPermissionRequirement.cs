using Microsoft.AspNetCore.Authorization;

namespace keynote_asp.AuthHandlers.AuthorizationRequirments
{
    public class HasPermissionRequirement : BaseAuthorizationRequirement
    {
        public string Permission { get; }

        public HasPermissionRequirement(string permission) : base()
        {
            Permission = permission;
        }
    }
}