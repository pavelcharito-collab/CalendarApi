namespace CalendarApi.Domain.Abstractions;

public interface ICalendarEventRepository
{
    void Add(CalendarEvent calendarEvent);
    Task<CalendarEvent?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    void Remove(CalendarEvent calendarEvent);
    IAsyncEnumerable<CalendarEvent> ListAsync(int skip, int take);
    IAsyncEnumerable<CalendarEvent> GetVisibleInRangeAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<bool> HasOverlapForParticipantAsync(
        Guid participantId, DateTimeOffset start, DateTimeOffset end,
        Guid? excludeEventId, CancellationToken cancellationToken = default);
}
