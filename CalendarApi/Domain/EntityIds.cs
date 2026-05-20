namespace CalendarApi.Domain;

public static class EntityIds
{
    public static Guid New() => Guid.CreateVersion7();
}
