namespace CalendarApi.DTO;

public sealed class ListEventsInRangeQuery
{
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}
