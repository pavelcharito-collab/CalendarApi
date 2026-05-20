using CalendarApi.Domain;
using CalendarApi.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Infrastructure.Persistence;

public partial class CalendarDbContext(DbContextOptions<CalendarDbContext> options)
    : DbContext(options), IUnitOfWork, IUserRepository, ICalendarEventRepository
{
    public DbSet<User> Users => Set<User>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CalendarEventConfiguration());
    }

    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        base.SaveChangesAsync(cancellationToken);
}
