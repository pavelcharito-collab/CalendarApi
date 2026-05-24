using CalendarApi.Domain.Exceptions;

namespace CalendarApi.Domain;

public class CalendarEvent
{
    private readonly List<Guid> _participantIds = [];

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public DateTimeOffset Start { get; private set; }
    public DateTimeOffset End { get; private set; }
    public RecurrencePattern? Recurrence { get; private set; }
    public DateTimeOffset SeriesEnd { get; private set; }
    public IReadOnlyList<Guid> ParticipantIds => _participantIds;

    private CalendarEvent() { }

    public static CalendarEvent Create(
        Guid ownerId, string title, string description,
        DateTimeOffset start, DateTimeOffset end,
        RecurrencePattern? recurrence)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }
        if (end <= start)
        {
            throw new DomainException("End must be after start.");
        }
        recurrence?.Validate(start);
        var calendarEvent = new CalendarEvent
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Title = title.Trim(),
            Description = description?.Trim() ?? "",
            Start = start,
            End = end,
            Recurrence = recurrence,
            SeriesEnd = RecurrenceSeriesBounds.ComputeSeriesEnd(start, end, recurrence)
        };
        calendarEvent._participantIds.Add(ownerId);
        return calendarEvent;
    }

    public void Update(string title, string description, DateTimeOffset start, DateTimeOffset end, RecurrencePattern? recurrence)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }
        if (end <= start)
        {
            throw new DomainException("End must be after start.");
        }
        recurrence?.Validate(start);
        Title = title.Trim();
        Description = description?.Trim() ?? "";
        Start = start;
        End = end;
        Recurrence = recurrence;
        SeriesEnd = RecurrenceSeriesBounds.ComputeSeriesEnd(start, end, recurrence);
    }

    public void AddParticipant(Guid userId)
    {
        if (!_participantIds.Contains(userId))
        {
            _participantIds.Add(userId);
        }
    }

    public bool IsVisibleTo(Guid userId) =>
        OwnerId == userId || _participantIds.Contains(userId);

    public TimeSpan Duration => End - Start;

    public DateTimeOffset SeriesRangeEnd() => SeriesEnd;
}
