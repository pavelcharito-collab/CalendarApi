namespace CalendarApi.Domain.Exceptions;

public sealed class ForbiddenException(string message) : DomainException(message);
