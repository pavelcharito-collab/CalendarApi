namespace CalendarApi.Domain.Exceptions;

public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException()
        : base("Missing or invalid X-User-Id header.")
    {
    }
}
