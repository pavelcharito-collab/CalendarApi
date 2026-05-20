namespace CalendarApi.Domain;

public interface IUserRepository
{
    void Add(User user);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int Total)> ListAsync(int skip, int take, CancellationToken cancellationToken = default);
}
