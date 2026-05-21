using CalendarApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Infrastructure.Persistence;

public static class CalendarEventQueries
{
    extension(IQueryable<CalendarEvent> source)
    {
        public IQueryable<CalendarEvent> FilterVisibleToUser(Guid userId) =>
            source.WhereIsParticipant(userId);

        public IQueryable<CalendarEvent> FilterForParticipant(Guid participantId, Guid? excludeEventId = null)
        {
            var q = source.WhereIsParticipant(participantId);
            if (excludeEventId is not null)
            {
                q = q.Where(e => e.Id != excludeEventId.Value);
            }

            return q;
        }

        public IQueryable<CalendarEvent> MayHaveInstancesInRange(DateTimeOffset from, DateTimeOffset to) =>
            source.Where(e => e.Recurrence == null
                ? e.Start < to && e.End > from
                : e.Start < to && (e.Recurrence.Until ?? e.Start.AddYears(2)) > from);

        private IQueryable<CalendarEvent> WhereIsParticipant(Guid userId)
        {
            var pattern = $"%,{userId},%";
            return source.Where(e => e.OwnerId == userId
                                     || EF.Functions.Like(
                                         "," + EF.Property<string>(e, nameof(CalendarEvent.ParticipantIds)) + ",",
                                         pattern));
        }
    }
}
