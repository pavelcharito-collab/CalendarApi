namespace CalendarApi.Domain;

public class User
{
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = "";

    private User() { }

    public static User Create(string displayName) =>
        new() { Id = EntityIds.New(), DisplayName = displayName.Trim() };
}
