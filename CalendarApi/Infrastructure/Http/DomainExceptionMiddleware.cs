using System.Net;
using System.Text.Json;
using CalendarApi.Domain.Exceptions;

namespace CalendarApi.Infrastructure.Http;

public sealed class DomainExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            var status = ex switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                ForbiddenException => HttpStatusCode.Forbidden,
                UnauthorizedException => HttpStatusCode.Unauthorized,
                ScheduleConflictException => HttpStatusCode.Conflict,
                _ => HttpStatusCode.BadRequest
            };

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";
            var problem = new
            {
                type = $"https://httpstatuses.com/{(int)status}",
                title = status.ToString(),
                status = (int)status,
                detail = ex.Message
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
