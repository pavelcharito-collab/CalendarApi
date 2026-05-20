namespace CalendarApi.DTO;

public record EventInstanceResponse(
    Guid SeriesId,
    string Title,
    string Description,
    Guid OwnerId,
    DateTimeOffset Start,
    DateTimeOffset End,
    IReadOnlyList<Guid> ParticipantIds);
