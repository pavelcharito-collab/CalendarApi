using CalendarApi.Domain;
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

    public async Task<(IReadOnlyList<User> Items, int Total)> ListAsync(int get, int skip, CancellationToken ct = default)
    {
        var (s, t) = Pagination.Normalize(get, skip);
        
        return await users.ListAsync(s, t, ct);
    }
}
