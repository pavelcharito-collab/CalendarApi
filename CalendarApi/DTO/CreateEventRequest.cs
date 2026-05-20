namespace CalendarApi.DTO;

public record CreateEventRequest(
    string Title, string Description,
    DateTimeOffset Start, DateTimeOffset End,
    RecurrencePatternDto? Recurrence);
