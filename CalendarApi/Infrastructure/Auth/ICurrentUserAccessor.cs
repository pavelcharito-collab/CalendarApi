namespace CalendarApi.Infrastructure.Auth;

public interface ICurrentUserAccessor
{
    Guid UserId { get; }
}
