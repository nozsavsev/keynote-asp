using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using keynote_asp.Helpers;
using keynote_asp.Services;
using System.Text.RegularExpressions;

namespace keynote_asp.SignalRHubs
{
    [Authorize("allowNoEmail")]
    public class AuthHub() : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            var user = Context.GetHttpContext()?.GetNauthUser() ?? throw new InvalidOperationException("User cannot be null on an authorized hub.");

            var sub = user.Id?.ToString() ?? throw new InvalidOperationException("User ID cannot be null.");
            await Groups.AddToGroupAsync(Context.ConnectionId, sub);

            var session = Context.GetHttpContext()?.GetNauthSession() ?? throw new InvalidOperationException("Session cannot be null on an authorized hub.");
            var sid = session.Id?.ToString() ?? throw new InvalidOperationException("Session ID cannot be null.");
            await Groups.AddToGroupAsync(Context.ConnectionId, sid);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            var user = Context.GetHttpContext()?.GetNauthUser() ?? throw new InvalidOperationException("User cannot be null on an authorized hub.");

            var sub = user.Id?.ToString() ?? throw new InvalidOperationException("User ID cannot be null.");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sub);

            var session = Context.GetHttpContext()?.GetNauthSession() ?? throw new InvalidOperationException("Session cannot be null on an authorized hub.");
            var sid = session.Id?.ToString() ?? throw new InvalidOperationException("Session ID cannot be null.");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sid);
        }
    }
}
