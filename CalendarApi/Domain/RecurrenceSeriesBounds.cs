namespace CalendarApi.Domain;

public static class RecurrenceSeriesBounds
{
    public static DateTimeOffset ComputeSeriesEnd(
        DateTimeOffset seriesStart,
        DateTimeOffset seriesEnd,
        RecurrencePattern? recurrence)
    {
        if (recurrence is null)
        {
            return seriesEnd;
        }

        recurrence.Validate(seriesStart);

        if (recurrence.Until is not null)
        {
            return recurrence.Until.Value;
        }

        if (recurrence.Count is not null)
        {
            var lastStart = ComputeLastOccurrenceStart(seriesStart, recurrence);
            return lastStart + (seriesEnd - seriesStart);
        }

        return seriesStart.AddYears(2);
    }

    private static DateTimeOffset ComputeLastOccurrenceStart(
        DateTimeOffset seriesStart,
        RecurrencePattern pattern)
    {
        pattern.Validate(seriesStart);
        if (pattern.Count is null)
        {
            throw new InvalidOperationException("Count is required to compute last occurrence.");
        }

        var cursor = seriesStart;
        for (var i = 1; i < pattern.Count.Value; i++)
        {
            cursor = RecurrenceCursor.Advance(cursor, pattern);
        }

        return cursor;
    }
}
