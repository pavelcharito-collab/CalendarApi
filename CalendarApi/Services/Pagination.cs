namespace CalendarApi.Services;

public static class Pagination
{
    public const int DefaultTake = 50;
    public const int MaxTake = 200;

    public static (int Skip, int Take) Normalize(int take, int skip)
    {
        var normalizedSkip = skip < 0 ? 0 : skip;
        var normalizedTake = take <= 0 || take >= int.MaxValue
            ? DefaultTake
            : take > MaxTake ? MaxTake : take;

        return (normalizedSkip, normalizedTake);
    }
}
