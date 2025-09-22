using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace keynote_asp
{
    public enum AuthFailureReasons
    {

        [JsonPropertyName("SessionExpired")]
        SessionExpired = 0,




        [JsonPropertyName("_2FARequired")]
        _2FARequired,

        [JsonPropertyName("RequireVerifiedEmail")]
        RequireVerifiedEmail,

        [JsonPropertyName("RequireEnabledUser")]
        RequireEnabledUser,

        [JsonPropertyName("Require2FASetup")]
        Require2FASetup,

        [JsonPropertyName("ForeginResource")]
        ForeginResource,


        //permissions
        [JsonPropertyName("PrManageUsers")]
        PrManageUsers,

        [JsonPropertyName("PrManageOwnServices")] //allows user to create and manage services
        PrManageOwnServices,

        [JsonPropertyName("PrManageServices")] //allows access to global service managment console
        PrManageServices,

        [JsonPropertyName("PrManageEmailTemplates")]
        PrManageEmailTemplates,

    }

    public enum WrResponseStatus
    {
        [JsonPropertyName("InternalError")]
        InternalError = 0,

        [JsonPropertyName("Ok")]
        Ok,

        [JsonPropertyName("Forbidden")]
        Forbidden,

        [JsonPropertyName("_2FARequired")]
        _2FARequired,

        [JsonPropertyName("Unauthorized")]
        Unauthorized,

        [JsonPropertyName("NotFound")]
        NotFound,

        [JsonPropertyName("BadRequest")]
        BadRequest,

        [JsonPropertyName("Cooldown")]
        Cooldown,

        [JsonPropertyName("ServerDown")]
        ServerDown,


        //controller specific
        [JsonPropertyName("EmailNotAvailable")]
        EmailNotAvailable,

        [JsonPropertyName("InvalidEmail")]
        InvalidEmail,

        [JsonPropertyName("InvalidPassword")]
        InvalidPassword,

        [JsonPropertyName("RequireEnabledUser")]
        RequireEnabledUser,

        //email actions
        [JsonPropertyName("InvalidApplyToken")]
        InvalidApplyToken,

    }

    public class ResponseWrapper<R> where R : class?
    {
        public ResponseWrapper(WrResponseStatus status, [AllowNull] R response = null, List<AuthFailureReasons>? AuthenticationFailureReasons = null)
        {
            this.status = status;
            this.response = response;

            authenticationFailureReasons = AuthenticationFailureReasons;
        }
        public ResponseWrapper(string status, [AllowNull] R response = null)
        {
            this.status = (WrResponseStatus)Enum.Parse(typeof(WrResponseStatus), status);
            this.response = response;
        }

        public WrResponseStatus status { get; set; }
        public List<AuthFailureReasons>? authenticationFailureReasons { get; set; } = null;
        public R? response { get; set; }
    }
}
