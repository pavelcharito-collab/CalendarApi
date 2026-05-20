using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CalendarApi.Infrastructure.Auth;

namespace CalendarApi.Infrastructure.WebSockets;

public sealed class CalendarSyncMiddleware(RequestDelegate next, CalendarChangeNotifier notifier)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUserAccessor currentUser)
    {
        if (!context.Request.Path.Equals("/api/v1/ws/calendar", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            
            return;
        }

        var callerId = currentUser.UserId;
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[4096];
        var result = await socket.ReceiveAsync(buffer, context.RequestAborted);
        if (result.MessageType != WebSocketMessageType.Text)
        {
            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Expected JSON subscribe message.", context.RequestAborted);
            
            return;
        }

        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("userId", out var userIdEl)
            || !Guid.TryParse(userIdEl.GetString(), out var subscribeUserId)
            || subscribeUserId != callerId)
        {
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "userId must match X-User-Id.", context.RequestAborted);
            
            return;
        }

        notifier.Subscribe(subscribeUserId, socket);
        try
        {
            while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
            {
                result = await socket.ReceiveAsync(buffer, context.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        finally
        {
            notifier.Unsubscribe(subscribeUserId, socket);
        }
    }
}
