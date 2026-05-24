using System.Text.Json;
using CalendarApi.Domain.Abstractions;
using CalendarApi.Infrastructure.Auth;
using CalendarApi.Infrastructure.Http;
using CalendarApi.Infrastructure.Persistence;
using CalendarApi.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CalendarDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CalendarDbContext>());
builder.Services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<CalendarDbContext>());
builder.Services.AddScoped<ICalendarEventRepository>(sp => sp.GetRequiredService<CalendarDbContext>());
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CalendarEventSchedulingService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => new
                {
                    field = e.Key,
                    message = err.ErrorMessage
                }))
                .ToList();

            var problem = new
            {
                type = "https://httpstatuses.com/400",
                title = "Bad Request",
                status = 400,
                detail = "One or more validation errors occurred.",
                errors
            };

            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            var problem = new
            {
                type = "https://httpstatuses.com/400",
                title = "Bad Request",
                status = 400,
                detail = "One or more validation errors occurred.",
                errors = validationException.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    });
});

app.UseMiddleware<DomainExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Calendar API v1"));
}

app.MapControllers();

app.Run();
