using CalendarApi.Domain.Exceptions;

namespace CalendarApi.Domain;

public static class RecurrenceCursor
{
    public static DateTimeOffset Advance(DateTimeOffset current, RecurrencePattern pattern) =>
        pattern.Frequency switch
        {
            RecurrenceFrequency.Daily => current.AddDays(pattern.Interval),
            RecurrenceFrequency.Weekly => current.AddDays(7 * pattern.Interval),
            RecurrenceFrequency.Monthly => current.AddMonths(pattern.Interval),
            _ => throw new DomainException("Unknown recurrence frequency.")
        };
}
