namespace CalendarApi.Services;

public static class Pagination
{
    public static (int Skip, int Take) Normalize(int take, int skip)
    {
        var normalizedSkip = skip >= int.MaxValue || skip < 0 ? 0 : skip;
        var t = take <= 0 || take >= int.MaxValue ? int.MaxValue : take;

        return (normalizedSkip, t);
    }
}
