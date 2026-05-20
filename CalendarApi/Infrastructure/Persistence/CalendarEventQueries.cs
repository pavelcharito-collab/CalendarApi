using CalendarApi.Domain;

namespace CalendarApi.Infrastructure.Persistence;

public static class CalendarEventQueries
{
    public static bool MayHaveInstancesInRange(CalendarEvent e, DateTimeOffset from, DateTimeOffset to) =>
        e.Recurrence is null
            ? e.Start < to && e.End > from
            : e.Start < to && e.SeriesRangeEnd() > from;

    private static bool ParticipantIdContains(CalendarEvent e, Guid userId)
    {
        var marker = $",{userId},";
        var csv = $",{string.Join(',', e.ParticipantIds)},";
        return csv.Contains(marker, StringComparison.Ordinal);
    }

    extension(IEnumerable<CalendarEvent> source)
    {
        public IEnumerable<CalendarEvent> FilterVisibleToUser(Guid userId) =>
            source.Where(e => IsParticipant(e, userId));

        public IEnumerable<CalendarEvent> FilterForParticipant(Guid participantId, Guid? excludeEventId = null)
        {
            var q = source.Where(e => IsParticipant(e, participantId));
            if (excludeEventId is not null)
            {
                q = q.Where(e => e.Id != excludeEventId.Value);
            }

            return q;
        }
    }

    private static bool IsParticipant(CalendarEvent e, Guid userId) =>
        e.OwnerId == userId || ParticipantIdContains(e, userId);
}
