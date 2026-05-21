namespace CalendarApi.Domain.Abstractions;

public interface ICalendarEventRepository
{
    void Add(CalendarEvent calendarEvent);
    Task<CalendarEvent?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    void Remove(CalendarEvent calendarEvent);
    IAsyncEnumerable<CalendarEvent> ListAsync(
        int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalendarEvent>> GetVisibleInRangeAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<bool> HasOverlapForParticipantAsync(
        Guid participantId, DateTimeOffset start, DateTimeOffset end,
        Guid? excludeEventId, CancellationToken cancellationToken = default);
}
