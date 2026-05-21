using CalendarApi.Domain.Abstractions;
using CalendarApi.Infrastructure.Auth;
using CalendarApi.Infrastructure.Http;
using CalendarApi.Infrastructure.Persistence;
using CalendarApi.Infrastructure.WebSockets;
using CalendarApi.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CalendarDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CalendarDbContext>());
builder.Services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<CalendarDbContext>());
builder.Services.AddScoped<ICalendarEventRepository>(sp => sp.GetRequiredService<CalendarDbContext>());
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CalendarEventSchedulingService>();
builder.Services.AddSingleton<CalendarChangeNotifier>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
    db.Database.Migrate();
}

app.UseWebSockets();
app.UseMiddleware<DomainExceptionMiddleware>();
app.UseMiddleware<CalendarSyncMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Calendar API v1"));
}

app.MapControllers();

app.Run();
