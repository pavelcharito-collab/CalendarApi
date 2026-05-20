using CalendarApi.Domain;
using CalendarApi.Services;

namespace CalendarApi.DTO.Mappers;

public static class DtoMapper
{
    public static UserResponse ToResponse(User user) =>
        new(user.Id, user.DisplayName);

    public static EventResponse ToResponse(CalendarEvent e) =>
        new(e.Id, e.OwnerId, e.Title, e.Description, e.Start, e.End,
            e.Recurrence is null ? null : ToDto(e.Recurrence), e.ParticipantIds);

    public static EventInstanceResponse ToResponse(EventInstance i) =>
        new(i.SeriesId, i.Title, i.Description, i.OwnerId, i.Start, i.End, i.ParticipantIds);

    public static RecurrencePattern? ToDomain(RecurrencePatternDto? dto)
    {
        if (dto is null) return null;
        if (!Enum.TryParse<RecurrenceFrequency>(dto.Frequency, ignoreCase: true, out var frequency))
        {
            throw new Domain.Exceptions.DomainException($"Unknown recurrence frequency: {dto.Frequency}");
        }
        
        return new RecurrencePattern
        {
            Frequency = frequency,
            Interval = dto.Interval,
            Until = dto.Until,
            Count = dto.Count
        };
    }

    private static RecurrencePatternDto ToDto(RecurrencePattern pattern) =>
        new(pattern.Frequency.ToString(), pattern.Interval, pattern.Until, pattern.Count);
}
