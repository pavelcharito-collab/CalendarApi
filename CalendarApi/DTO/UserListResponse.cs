namespace CalendarApi.DTO;

public record UserListResponse(int TotalCount, int PageSize, IReadOnlyList<UserResponse> Items);
