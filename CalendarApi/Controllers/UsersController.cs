using CalendarApi.Domain.Exceptions;
using CalendarApi.DTO;
using CalendarApi.DTO.Mappers;
using CalendarApi.Infrastructure.Auth;
using CalendarApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CalendarApi.Controllers;

/// <summary>
/// User registration and lookup under <c>api/v1/users</c>.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController(UserService users, ICurrentUserAccessor current) : ControllerBase
{
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="request">Display name for the new user.</param>
    /// <returns>The created user with a version-7 GUID.</returns>
    /// <response code="201">User created.</response>
    /// <response code="400">Validation failed or display name is missing.</response>
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await users.CreateAsync(request.DisplayName, ct);

        return CreatedAtAction(nameof(Get), new { userId = user.Id }, DtoMapper.ToResponse(user));
    }

    /// <summary>
    /// Gets a user by ID (caller may only read their own profile).
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>The user profile.</returns>
    /// <response code="200">User found.</response>
    /// <response code="401">Missing or invalid <c>X-User-Id</c> header.</response>
    /// <response code="403">Caller is not requesting their own profile.</response>
    /// <response code="404">User does not exist.</response>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserResponse>> Get(Guid userId, CancellationToken ct)
    {
        if (userId != current.UserId)
        {
            throw new ForbiddenException("Cannot access another user's profile.");
        }

        var user = await users.GetAsync(userId, ct);

        return DtoMapper.ToResponse(user);
    }

    /// <summary>
    /// Lists users with optional pagination (intended for development and testing).
    /// </summary>
    /// <param name="take">Maximum rows to return; default 50, maximum 200.</param>
    /// <param name="skip">Rows to skip; default <c>0</c>.</param>
    /// <returns>Object with <c>totalCount</c>, <c>pageSize</c>, and <c>items</c> array.</returns>
    /// <response code="200">Paged user list.</response>
    [HttpGet]
    public async Task<ActionResult<UserListResponse>> List(
        [FromQuery] int take = Pagination.DefaultTake,
        [FromQuery] int skip = 0,
        CancellationToken ct = default)
    {
        var totalCount = await users.CountAllAsync(ct);
        var items = await users.ListAllAsync(take, skip)
            .Select(DtoMapper.ToResponse)
            .ToListAsync(ct);

        return Ok(new UserListResponse(totalCount, items.Count, items));
    }
}
