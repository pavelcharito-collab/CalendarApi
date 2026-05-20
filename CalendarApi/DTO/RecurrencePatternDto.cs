namespace CalendarApi.DTO;

public record RecurrencePatternDto(
    string Frequency, int Interval, DateTimeOffset? Until, int? Count);
