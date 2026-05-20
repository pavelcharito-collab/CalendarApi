using CalendarApi.Domain;
using CalendarApi.Domain.Exceptions;

namespace CalendarApi.Services;

public static class RecurrenceExpander
{
    private const int MaxInstances = 500;

    public static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> Expand(
        CalendarEvent series, DateTimeOffset rangeFrom, DateTimeOffset rangeTo)
    {
        if (series.Recurrence is null)
        {
            if (series.Start < rangeTo && series.End > rangeFrom)
            {
                yield return (series.Start, series.End);
            }
            yield break;
        }

        var pattern = series.Recurrence;
        pattern.Validate();
        var duration = series.Duration;
        var cursor = series.Start;
        var count = 0;

        while (cursor < rangeTo && count < MaxInstances)
        {
            var instanceEnd = cursor + duration;
            if (pattern.Until is not null && cursor > pattern.Until.Value) break;
            if (pattern.Count is not null && count >= pattern.Count.Value) break;

            if (instanceEnd > rangeFrom && cursor < rangeTo)
            {
                yield return (cursor, instanceEnd);
            }

            cursor = Advance(cursor, pattern);
            count++;
        }
    }

    private static DateTimeOffset Advance(DateTimeOffset current, RecurrencePattern pattern) =>
        pattern.Frequency switch
        {
            RecurrenceFrequency.Daily => current.AddDays(pattern.Interval),
            RecurrenceFrequency.Weekly => current.AddDays(7 * pattern.Interval),
            RecurrenceFrequency.Monthly => current.AddMonths(pattern.Interval),
            _ => throw new DomainException("Unknown recurrence frequency.")
        };
}
