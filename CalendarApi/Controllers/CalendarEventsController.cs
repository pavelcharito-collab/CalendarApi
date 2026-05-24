using CalendarApi.DTO;
using CalendarApi.DTO.Mappers;
using CalendarApi.Infrastructure.Auth;
using CalendarApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CalendarApi.Controllers;

/// <summary>
/// Calendar events: CRUD, invitations, and time-range queries under <c>api/v1</c>.
/// Mutating endpoints require the <c>X-User-Id</c> header (authenticated caller).
/// </summary>
[ApiController]
[Route("api/v1")]
public class CalendarEventsController(CalendarEventSchedulingService scheduling, ICurrentUserAccessor current) : ControllerBase
{
    /// <summary>
    /// Creates a calendar event owned by the caller.
    /// </summary>
    /// <param name="request">Title, description, start/end, and optional recurrence.</param>
    /// <returns>The created event.</returns>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed or missing <c>X-User-Id</c>.</response>
    /// <response code="409">Event overlaps another event for the same owner.</response>
    [HttpPost("events")]
    public async Task<ActionResult<EventResponse>> Create(CreateEventRequest request, CancellationToken ct)
    {
        var calendarEvent = await scheduling.CreateAsync(
            current.UserId, request.Title, request.Description,
            request.Start, request.End, DtoMapper.ToDomain(request.Recurrence), ct);
        
        return CreatedAtAction(nameof(Get), new { eventId = calendarEvent.Id }, DtoMapper.ToResponse(calendarEvent));
    }

    /// <summary>
    /// Lists all events with optional pagination (intended for development and testing; no auth required).
    /// </summary>
    /// <param name="take">Maximum rows to return; default <c>int.MaxValue</c> returns all events.</param>
    /// <param name="skip">Rows to skip; default <c>0</c>.</param>
    /// <returns>Object with <c>count</c> and <c>items</c> array.</returns>
    /// <response code="200">Paged or full event list.</response>
    [HttpGet("events")]
    public async Task<ActionResult<EventListResponse>> List([FromQuery] int take = int.MaxValue, [FromQuery] int skip = 0, CancellationToken ct = default)
    {
        var items = await scheduling.ListAllAsync(take, skip)
            .Select(DtoMapper.ToResponse)
            .ToListAsync(ct);

        return Ok(new EventListResponse (items.Count, items));
    }

    /// <summary>
    /// Gets a single event by ID.
    /// </summary>
    /// <param name="eventId">Event identifier.</param>
    /// <returns>Event details including participants and recurrence.</returns>
    /// <response code="200">Event found and caller is owner or participant.</response>
    /// <response code="400">Missing or invalid <c>X-User-Id</c> header.</response>
    /// <response code="403">Caller is not owner or participant.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{eventId:guid}")]
    public async Task<ActionResult<EventResponse>> Get(Guid eventId, CancellationToken ct)
    {
        var calendarEvent = await scheduling.GetAsync(current.UserId, eventId, ct);
        
        return DtoMapper.ToResponse(calendarEvent);
    }

    /// <summary>
    /// Updates an event (owner only; applies to the whole recurrence series).
    /// </summary>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="request">Updated title, description, times, and recurrence.</param>
    /// <returns>The updated event.</returns>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation failed or missing <c>X-User-Id</c>.</response>
    /// <response code="403">Caller is not the owner.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">Update would overlap another event for the owner.</response>
    [HttpPut("events/{eventId:guid}")]
    public async Task<ActionResult<EventResponse>> Update(Guid eventId, UpdateEventRequest request, CancellationToken ct)
    {
        var calendarEvent = await scheduling.UpdateAsync(
            current.UserId, eventId, request.Title, request.Description,
            request.Start, request.End, DtoMapper.ToDomain(request.Recurrence), ct);
        
        return DtoMapper.ToResponse(calendarEvent);
    }

    /// <summary>
    /// Deletes an event (owner only).
    /// </summary>
    /// <param name="eventId">Event identifier.</param>
    /// <response code="204">Event deleted.</response>
    /// <response code="400">Missing or invalid <c>X-User-Id</c> header.</response>
    /// <response code="403">Caller is not the owner.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpDelete("events/{eventId:guid}")]
    public async Task<IActionResult> Delete(Guid eventId, CancellationToken ct)
    {
        await scheduling.DeleteAsync(current.UserId, eventId, ct);
        
        return NoContent();
    }

    /// <summary>
    /// Invites a user to participate in an event (owner only).
    /// </summary>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="request">User ID of the invitee.</param>
    /// <returns>Event with updated participant list.</returns>
    /// <response code="200">Participant added.</response>
    /// <response code="400">Validation failed or missing <c>X-User-Id</c>.</response>
    /// <response code="403">Caller is not the owner.</response>
    /// <response code="404">Event or invitee user not found.</response>
    /// <response code="409">Invitee has a conflicting event in the same time window.</response>
    [HttpPost("events/{eventId:guid}/participants")]
    public async Task<ActionResult<EventResponse>> Invite(Guid eventId, InviteParticipantRequest request, CancellationToken ct)
    {
        var calendarEvent = await scheduling.InviteAsync(current.UserId, eventId, request.UserId, ct);
        
        return DtoMapper.ToResponse(calendarEvent);
    }

    /// <summary>
    /// Lists expanded event instances for a user within a time range.
    /// </summary>
    /// <param name="userId">Calendar owner; must match the <c>X-User-Id</c> header.</param>
    /// <param name="from">Range start (inclusive).</param>
    /// <param name="to">Range end (exclusive).</param>
    /// <returns>Occurrences visible to the caller (owned or invited), with recurrence expanded.</returns>
    /// <response code="200">Instances in range.</response>
    /// <response code="400">Missing or invalid <c>X-User-Id</c> header.</response>
    /// <response code="403"><paramref name="userId"/> does not match the caller.</response>
    [HttpGet("users/{userId:guid}/events")]
    public async Task<ActionResult<IReadOnlyList<EventInstanceResponse>>> ListForUser(
        Guid userId, [FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken ct)
    {
        var items = await scheduling.ListForUserInRangeAsync(current.UserId, userId, from, to, ct)
            .OrderBy(i => i.Start)
            .Select(DtoMapper.ToResponse)
            .ToListAsync(ct);

        return Ok(items);
    }
}
