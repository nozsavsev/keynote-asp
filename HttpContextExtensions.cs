using Keynote_asp.Nauth.API_GEN.Models;
using keynote_asp.Models.User;

namespace keynote_asp
{
    public static class HttpContextExtensions
    {
        // Nauth User and Session
        public static UserDTO? GetNauthUser(this HttpContext context)
        {
            return context.Items.TryGetValue("NauthUser", out var user) ? user as UserDTO : null;
        }

        public static FullSessionDTO? GetNauthSession(this HttpContext context)
        {
            return context.Items.TryGetValue("NauthSession", out var session) ? session as FullSessionDTO : null;
        }

        // Keynote User
        public static DB_User? GetKeynoteUser(this HttpContext context)
        {
            return context.Items.TryGetValue("KeynoteUser", out var user) ? user as DB_User : null;
        }

        // Authentication failure reasons
        public static void AddAuthenticationFailureReason(this HttpContext context, AuthFailureReasons reason)
        {
            if (!context.Items.ContainsKey("AuthFailureReasons"))
            {
                context.Items["AuthFailureReasons"] = new List<AuthFailureReasons>();
            }

            if (context.Items["AuthFailureReasons"] is List<AuthFailureReasons> reasons)
            {
                reasons.Add(reason);
            }
        }

        public static List<AuthFailureReasons>? GetAuthenticationFailureReasons(this HttpContext context)
        {
            return context.Items.TryGetValue("AuthFailureReasons", out var reasons) 
                ? reasons as List<AuthFailureReasons> 
                : null;
        }
    }
}