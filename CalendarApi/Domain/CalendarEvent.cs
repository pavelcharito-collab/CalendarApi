using CalendarApi.Domain.Exceptions;

namespace CalendarApi.Domain;

public class CalendarEvent
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public DateTimeOffset Start { get; private set; }
    public DateTimeOffset End { get; private set; }
    public RecurrencePattern? Recurrence { get; private set; }
    public List<Guid> ParticipantIds { get; private init; } = [];

    private CalendarEvent() { }

    public static CalendarEvent Create(
        Guid ownerId, string title, string description,
        DateTimeOffset start, DateTimeOffset end,
        RecurrencePattern? recurrence)
    {
        if (end <= start)
        {
            throw new DomainException("End must be after start.");
        }
        recurrence?.Validate();
        return new CalendarEvent
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Title = title.Trim(),
            Description = description?.Trim() ?? "",
            Start = start,
            End = end,
            Recurrence = recurrence,
            ParticipantIds = [ownerId]
        };
    }

    public void Update(string title, string description, DateTimeOffset start, DateTimeOffset end, RecurrencePattern? recurrence)
    {
        if (end <= start)
        {
            throw new DomainException("End must be after start.");
        }
        recurrence?.Validate();
        Title = title.Trim();
        Description = description?.Trim() ?? "";
        Start = start;
        End = end;
        Recurrence = recurrence;
    }

    public void AddParticipant(Guid userId)
    {
        if (!ParticipantIds.Contains(userId))
        {
            ParticipantIds.Add(userId);
        }
    }

    public bool IsVisibleTo(Guid userId) =>
        OwnerId == userId || ParticipantIds.Contains(userId);

    public TimeSpan Duration => End - Start;

    public DateTimeOffset SeriesRangeEnd() => Recurrence?.Until ?? Start.AddYears(2);
}
