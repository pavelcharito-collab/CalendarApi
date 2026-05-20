using CalendarApi.Domain;
using CalendarApi.Domain.Exceptions;
using CalendarApi.Infrastructure.WebSockets;

namespace CalendarApi.Services;

public sealed record EventInstance(
    Guid SeriesId,
    string Title,
    string Description,
    Guid OwnerId,
    DateTimeOffset Start,
    DateTimeOffset End,
    IReadOnlyList<Guid> ParticipantIds);

public sealed class CalendarEventSchedulingService(
    ICalendarEventRepository calendarEvents,
    IUserRepository users,
    IUnitOfWork uow,
    CalendarChangeNotifier notifier)
{
    public async Task<CalendarEvent> CreateAsync(
        Guid callerId, string title, string description,
        DateTimeOffset start, DateTimeOffset end,
        RecurrencePattern? recurrence, CancellationToken ct = default)
    {
        _ = await users.GetByIdAsync(callerId, ct) ?? throw new NotFoundException("User not found.");
        var calendarEvent = CalendarEvent.Create(callerId, title, description, start, end, recurrence);
        await EnsureNoConflictsAsync(calendarEvent, excludeEventId: null, ct);
        calendarEvents.Add(calendarEvent);
        await uow.SaveChangesAsync(ct);
        await NotifyChangeAsync(calendarEvent, "created", ct);
        
        return calendarEvent;
    }

    public async Task<CalendarEvent> GetAsync(Guid callerId, Guid eventId, CancellationToken ct = default)
    {
        var calendarEvent = await calendarEvents.GetByIdForUpdateAsync(eventId, ct);
        if (calendarEvent is null)
        {
            throw new NotFoundException("Event not found.");
        }
        
        return calendarEvent.IsVisibleTo(callerId)
            ? calendarEvent
            : throw new ForbiddenException("Event is not visible to this user.");
    }

    public async Task<CalendarEvent> UpdateAsync(
        Guid callerId, Guid eventId, string title, string description,
        DateTimeOffset start, DateTimeOffset end,
        RecurrencePattern? recurrence, CancellationToken ct = default)
    {
        var calendarEvent = await RequireOwnerAsync(callerId, eventId, ct);
        calendarEvent.Update(title, description, start, end, recurrence);
        await EnsureNoConflictsAsync(calendarEvent, excludeEventId: eventId, ct);
        await uow.SaveChangesAsync(ct);
        await NotifyChangeAsync(calendarEvent, "updated", ct);
        
        return calendarEvent;
    }

    public async Task DeleteAsync(Guid callerId, Guid eventId, CancellationToken ct = default)
    {
        var calendarEvent = await RequireOwnerAsync(callerId, eventId, ct);
        calendarEvents.Remove(calendarEvent);
        await uow.SaveChangesAsync(ct);
        await NotifyChangeAsync(calendarEvent, "deleted", ct);
    }

    public async Task<CalendarEvent> InviteAsync(
        Guid callerId, Guid eventId, Guid inviteeId, CancellationToken ct = default)
    {
        var calendarEvent = await RequireOwnerAsync(callerId, eventId, ct);
        _ = await users.GetByIdAsync(inviteeId, ct) ?? throw new NotFoundException("Invitee not found.");
        calendarEvent.AddParticipant(inviteeId);
        await EnsureNoConflictsAsync(calendarEvent, excludeEventId: eventId, ct);
        await uow.SaveChangesAsync(ct);
        await NotifyChangeAsync(calendarEvent, "updated", ct);
        
        return calendarEvent;
    }

    public async Task<(IReadOnlyList<CalendarEvent> Items, int Total)> ListAllAsync(
        int take, int skip, CancellationToken ct = default)
    {
        var (s, t) = Pagination.Normalize(take, skip);

        return await calendarEvents.ListAsync(s, t, ct);
    }

    public async Task<IReadOnlyList<EventInstance>> ListForUserInRangeAsync(
        Guid callerId, Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        if (callerId != userId)
        {
            throw new ForbiddenException("Cannot query another user's calendar.");
        }
        var series = await calendarEvents.GetVisibleInRangeAsync(userId, from, to, ct);
        var instances = new List<EventInstance>();
        foreach (var s in series)
        {
            foreach (var (start, end) in RecurrenceExpander.Expand(s, from, to))
            {
                instances.Add(new EventInstance(
                    s.Id, s.Title, s.Description, s.OwnerId, start, end, s.ParticipantIds));
            }
        }

        return instances.OrderBy(i => i.Start).ToList();
    }

    private async Task<CalendarEvent> RequireOwnerAsync(Guid callerId, Guid eventId, CancellationToken ct)
    {
        var calendarEvent = await calendarEvents.GetByIdForUpdateAsync(eventId, ct);
        if (calendarEvent is null)
        {
            throw new NotFoundException("Event not found.");
        }
        
        return calendarEvent.OwnerId == callerId
            ? calendarEvent
            : throw new ForbiddenException("Only the event owner can perform this action.");
    }

    private async Task EnsureNoConflictsAsync(
        CalendarEvent candidate, Guid? excludeEventId, CancellationToken ct)
    {
        var (checkFrom, checkTo) = ConflictWindow(candidate);
        var instances = RecurrenceExpander.Expand(candidate, checkFrom, checkTo).ToList();
        foreach (var participantId in candidate.ParticipantIds.Distinct())
        {
            foreach (var (start, end) in instances)
            {
                if (await calendarEvents.HasOverlapForParticipantAsync(participantId, start, end, excludeEventId, ct))
                {
                    throw new ScheduleConflictException(
                        $"Schedule conflict for participant {participantId} between {start:o} and {end:o}.");
                }
            }
        }
    }

    private static (DateTimeOffset From, DateTimeOffset To) ConflictWindow(CalendarEvent e) =>
        (e.Start, e.SeriesRangeEnd());

    private async Task NotifyChangeAsync(CalendarEvent calendarEvent, string action, CancellationToken ct)
    {
        var payload = new { action, eventId = calendarEvent.Id, userId = calendarEvent.OwnerId };
        await notifier.NotifyParticipantsAsync(calendarEvent.ParticipantIds, payload, ct);
    }
}
