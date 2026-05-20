namespace CalendarApi.DTO;

public record UpdateEventRequest(
    string Title, string Description,
    DateTimeOffset Start, DateTimeOffset End,
    RecurrencePatternDto? Recurrence);
