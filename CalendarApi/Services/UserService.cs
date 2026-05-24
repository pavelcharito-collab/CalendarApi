using CalendarApi.Domain;
using CalendarApi.Domain.Abstractions;
using CalendarApi.Domain.Exceptions;

namespace CalendarApi.Services;

public sealed class UserService(IUserRepository users, IUnitOfWork uow)
{
    public async Task<User> CreateAsync(string displayName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("Display name is required.");
        }
        var user = User.Create(displayName);
        users.Add(user);
        await uow.SaveChangesAsync(ct);

        return user;
    }

    public async Task<User> GetAsync(Guid id, CancellationToken ct = default) =>
        await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User not found.");

    public IAsyncEnumerable<User> ListAllAsync(int take, int skip)
    {
        var (s, t) = Pagination.Normalize(take, skip);

        return users.ListAsync(s, t);
    }

    public Task<int> CountAllAsync(CancellationToken ct = default) =>
        users.CountAsync(ct);
}
