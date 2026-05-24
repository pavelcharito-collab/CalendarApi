using CalendarApi.Domain.Exceptions;

namespace CalendarApi.Domain;

public sealed class RecurrencePattern
{
    public RecurrenceFrequency Frequency { get; init; }
    public int Interval { get; init; } = 1;
    public DateTimeOffset? Until { get; init; }
    public int? Count { get; init; }

    public void Validate() => Validate(DateTimeOffset.MinValue);

    public void Validate(DateTimeOffset seriesStart)
    {
        if (Interval < 1) throw new DomainException("Recurrence interval must be at least 1.");
        if (Until is null && Count is null) throw new DomainException("Recurrence requires Until or Count.");
        if (Until is not null && Count is not null) throw new DomainException("Recurrence cannot have both Until and Count.");
        if (Count is < 1) throw new DomainException("Recurrence count must be at least 1.");
        if (Until is not null && seriesStart != DateTimeOffset.MinValue && Until.Value < seriesStart)
        {
            throw new DomainException("Recurrence Until must be on or after series start.");
        }
    }
}
