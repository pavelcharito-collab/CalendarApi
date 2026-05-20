using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CalendarApi.Infrastructure.WebSockets;

public sealed class CalendarChangeNotifier
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, WebSocket>> _connections = new();

    public void Subscribe(Guid userId, WebSocket socket)
    {
        var userConnections = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, WebSocket>());
        userConnections[Guid.CreateVersion7()] = socket;
    }

    public void Unsubscribe(Guid userId, WebSocket socket)
    {
        if (!_connections.TryGetValue(userId, out var userConnections))
        {
            return;
        }
        foreach (var (id, ws) in userConnections)
        {
            if (ReferenceEquals(ws, socket))
            {
                userConnections.TryRemove(id, out _);
            }
        }
    }
    
    public async Task NotifyParticipantsAsync(
        IEnumerable<Guid> participantIds, object payload, CancellationToken ct = default)
    {
        foreach (var userId in participantIds.Distinct())
        {
            await NotifyAsync(userId, payload, ct);
        }
    }

    private async Task NotifyAsync(Guid userId, object payload, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(userId, out var userConnections))
        {
            return;
        }
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (id, socket) in userConnections)
        {
            if (socket.State != WebSocketState.Open)
            {
                userConnections.TryRemove(id, out _);
                continue;
            }

            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
            }
            catch
            {
                userConnections.TryRemove(id, out _);
            }
        }
    }
}
