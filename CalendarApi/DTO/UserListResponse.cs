namespace CalendarApi.DTO;

public record UserListResponse(int Count, IReadOnlyList<UserResponse> Items);