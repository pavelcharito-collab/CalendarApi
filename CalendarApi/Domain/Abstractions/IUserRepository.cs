namespace CalendarApi.Domain.Abstractions;

public interface IUserRepository
{
    void Add(User user);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    IAsyncEnumerable<User> ListAsync(int skip, int take);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
