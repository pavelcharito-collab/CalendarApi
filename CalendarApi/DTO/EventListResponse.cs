namespace CalendarApi.DTO;

public record EventListResponse(int TotalCount, int PageSize, IReadOnlyList<EventResponse> Items);
