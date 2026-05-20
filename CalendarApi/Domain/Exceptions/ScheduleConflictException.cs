namespace CalendarApi.Domain.Exceptions;

public sealed class ScheduleConflictException(string message)
    : DomainException(message);
