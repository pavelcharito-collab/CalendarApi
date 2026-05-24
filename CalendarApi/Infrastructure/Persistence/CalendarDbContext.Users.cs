using CalendarApi.Domain;
using CalendarApi.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Infrastructure.Persistence;

public partial class CalendarDbContext
{
    public void Add(User user) => Users.Add(user);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    Task<int> IUserRepository.CountAsync(CancellationToken cancellationToken) =>
        Users.AsNoTracking().CountAsync(cancellationToken);

    IAsyncEnumerable<User> IUserRepository.ListAsync(int skip, int take)
    {
        IQueryable<User> q = Users.AsNoTracking().OrderBy(u => u.Id);
        if (skip > 0)
        {
            q = q.Skip(skip);
        }
        if (take > 0)
        {
            q = q.Take(take);
        }

        return q.AsAsyncEnumerable();
    }
}
