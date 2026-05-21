using CalendarApi.Domain;
using CalendarApi.Services;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Infrastructure.Persistence;

public partial class CalendarDbContext
{
    public void Add(CalendarEvent calendarEvent) => CalendarEvents.Add(calendarEvent);

    public Task<CalendarEvent?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        CalendarEvents.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Remove(CalendarEvent calendarEvent) => CalendarEvents.Remove(calendarEvent);

    IAsyncEnumerable<CalendarEvent> ICalendarEventRepository.ListAsync(int skip, int take)
    {
        IQueryable<CalendarEvent> q = CalendarEvents.AsNoTracking().OrderByDescending(e => e.Id); //    .OrderBy(e => e.Start); //  SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        if (skip > 0)
        {
            q = q.Skip(skip);
        }
        if (take > 0)
        {
            q = q.Take(take);
        }

        return q.AsAsyncEnumerable();
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetVisibleInRangeAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var items = await CalendarEvents.AsNoTracking()
            .FilterVisibleToUser(userId)
            .MayHaveInstancesInRange(from, to)
            .ToListAsync(cancellationToken);

        return items.OrderBy(e => e.Start).ToList();
    }

    public async Task<bool> HasOverlapForParticipantAsync(
        Guid participantId, DateTimeOffset start, DateTimeOffset end,
        Guid? excludeEventId, CancellationToken cancellationToken = default)
    {
        var candidates = CalendarEvents.AsNoTracking()
            .FilterForParticipant(participantId, excludeEventId)
            .MayHaveInstancesInRange(start, end);

        await foreach (var e in candidates.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (RecurrenceExpander.Expand(e, start, end).Any())
            {
                return true;
            }
        }

        return false;
    }
}
