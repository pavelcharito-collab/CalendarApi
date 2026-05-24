using CalendarApi.Domain;

namespace CalendarApi.Services;

public static class RecurrenceExpander
{
    private const int MaxInstances = 500;

    public static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> Expand(
        CalendarEvent series, DateTimeOffset rangeFrom, DateTimeOffset rangeTo) =>
        ExpandInternal(series, rangeFrom, rangeTo, maxInstances: MaxInstances, useSeriesEndBound: false);

    public static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> ExpandForConflictCheck(
        CalendarEvent series, DateTimeOffset rangeFrom, DateTimeOffset rangeTo) =>
        ExpandInternal(series, rangeFrom, rangeTo, maxInstances: null, useSeriesEndBound: true);

    private static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> ExpandInternal(
        CalendarEvent series,
        DateTimeOffset rangeFrom,
        DateTimeOffset rangeTo,
        int? maxInstances,
        bool useSeriesEndBound)
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
        pattern.Validate(series.Start);
        var duration = series.Duration;
        var cursor = series.Start;
        var count = 0;
        var loopEnd = useSeriesEndBound ? Max(rangeTo, series.SeriesEnd) : rangeTo;
        if (useSeriesEndBound && pattern.Until is not null)
        {
            loopEnd = Max(loopEnd, pattern.Until.Value.AddTicks(1));
        }

        while (cursor < loopEnd && (maxInstances is null || count < maxInstances.Value))
        {
            var instanceEnd = cursor + duration;
            if (pattern.Until is not null && cursor > pattern.Until.Value) break;
            if (pattern.Count is not null && count >= pattern.Count.Value) break;

            if (instanceEnd > rangeFrom && cursor < rangeTo)
            {
                yield return (cursor, instanceEnd);
            }

            cursor = RecurrenceCursor.Advance(cursor, pattern);
            count++;
        }
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) =>
        a >= b ? a : b;
}
