using CalendarApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Infrastructure.Persistence;

public partial class CalendarDbContext
{
    public void Add(User user) => Users.Add(user);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    async Task<(IReadOnlyList<User> Items, int Total)> IUserRepository.ListAsync(
        int skip, int take, CancellationToken cancellationToken)
    {
        IQueryable<User> q = Users.AsNoTracking().OrderBy(u => u.Id);
        var total = await q.CountAsync(cancellationToken);
        if (skip > 0)
        {
            q = q.Skip(skip);
        }
        if (take > 0)
        {
            q = q.Take(take);
        }
        var items = await q.ToListAsync(cancellationToken);
        
        return (items, total);
    }
}
