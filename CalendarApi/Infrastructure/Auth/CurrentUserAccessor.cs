using CalendarApi.Domain.Exceptions;

namespace CalendarApi.Infrastructure.Auth;

public sealed class CurrentUserAccessor(IHttpContextAccessor http) : ICurrentUserAccessor
{
    public Guid UserId =>
        http.HttpContext?.Request.Headers.TryGetValue("X-User-Id", out var v) == true
        && Guid.TryParse(v.FirstOrDefault(), out var id)
            ? id
            : throw new DomainException("Missing or invalid X-User-Id header.");
}
