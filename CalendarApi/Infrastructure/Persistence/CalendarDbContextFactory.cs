using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CalendarApi.Infrastructure.Persistence;

public class CalendarDbContextFactory : IDesignTimeDbContextFactory<CalendarDbContext>
{
    public CalendarDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CalendarDbContext>()
            .UseSqlite("Data Source=calendar.db")
            .Options;
        
        return new CalendarDbContext(options);
    }
}
