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

    async Task<(IReadOnlyList<CalendarEvent> Items, int Total)> ICalendarEventRepository.ListAsync(
        int skip, int take, CancellationToken cancellationToken)
    {
        IQueryable<CalendarEvent> q = CalendarEvents.AsNoTracking().OrderByDescending(e => e.Id); //    .OrderBy(e => e.Start); //  SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        var total = await q.CountAsync(cancellationToken);
        if (skip > 0)
        {
            q = q.Skip(skip);
        }
        if (take > 0)
        {
            q = q.Take(take);
        }
        var items = await q.ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetVisibleInRangeAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        // todo kh this is not good, need join in db
        var all = await CalendarEvents.AsNoTracking().ToListAsync(cancellationToken);
        return all
            .FilterVisibleToUser(userId)
            .Where(e => CalendarEventQueries.MayHaveInstancesInRange(e, from, to))
            .OrderBy(e => e.Start)
            .ToList();
    }

    public async Task<bool> HasOverlapForParticipantAsync(
        Guid participantId, DateTimeOffset start, DateTimeOffset end,
        Guid? excludeEventId, CancellationToken cancellationToken = default)
    {
        var candidates = await CalendarEvents.AsNoTracking().ToListAsync(cancellationToken);
        return candidates
            .FilterForParticipant(participantId, excludeEventId)
            .Any(e => RecurrenceExpander.Expand(e, start, end).Any());
    }
}
