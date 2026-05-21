namespace CalendarApi.DTO;

public record EventListResponse(int Count, IReadOnlyList<EventResponse> Items);