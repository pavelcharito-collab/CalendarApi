namespace CalendarApi.DTO;

public record EventResponse(
    Guid Id,
    Guid OwnerId,
    string Title,
    string Description,
    DateTimeOffset Start,
    DateTimeOffset End,
    RecurrencePatternDto? Recurrence,
    IReadOnlyList<Guid> ParticipantIds);
