using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    private IQueryable<Event> EventQuery() => _db.Events
        .Include(e => e.Venue)
        .Include(e => e.Organiser)
        .Include(e => e.Categories).ThenInclude(c => c.Enrolments);

    /// <summary>Lists all events, optionally filtered by date range or venue. Public.</summary>
    /// <remarks>Optional query parameters: from, to (dates, inclusive) and venueId. Ordered by date.</remarks>
    /// <response code="200">Array of events, each with its venue and categories.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery(Name = "from")] DateTime? fromDate, [FromQuery(Name = "to")] DateTime? toDate, [FromQuery] int? venueId)
    {
        var query = EventQuery().AsNoTracking();
        if (fromDate.HasValue)
        {
            var f = DateOnly.FromDateTime(fromDate.Value);
            query = query.Where(e => e.EventDate >= f);
        }
        if (toDate.HasValue)
        {
            var t = DateOnly.FromDateTime(toDate.Value);
            query = query.Where(e => e.EventDate <= t);
        }
        if (venueId.HasValue)
            query = query.Where(e => e.VenueId == venueId.Value);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => e.ToDto()).ToList());
    }

    /// <summary>Returns the full details of one event, including its venue and categories. Public.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await EventQuery().AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id);
        return ev is null ? NotFound(new ApiError("Event not found.")) : Ok(ev.ToDto());
    }

    /// <summary>Creates a new event owned by the logged-in Organiser. Organisers only.</summary>
    /// <remarks>
    /// Expects name, description (optional), eventDate (yyyy-MM-dd, not in the past), venueId
    /// (the venue gives the event's location) and eventType (Run, Walk or Cycle).
    /// Distances are defined per category.
    /// </remarks>
    /// <response code="201">Event created. Returns the event including its new ID.</response>
    /// <response code="400">Invalid data, unknown venue or date in the past.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] EventRequestDto dto)
    {
        var error = await ValidateAsync(dto, null);
        if (error is not null) return BadRequest(new ApiError(error));

        var ev = new Event
        {
            OrganiserId = User.GetUserId(),
            VenueId = dto.VenueId,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            EventDate = dto.EventDate,
            EventType = dto.EventType,
            CreatedAt = DateTime.Now
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var created = await EventQuery().AsNoTracking().FirstAsync(e => e.EventId == ev.EventId);
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, created.ToDto());
    }

    /// <summary>Updates an event. Only the Organiser who owns the event may do this.</summary>
    /// <remarks>Expects the same body as event creation.</remarks>
    /// <response code="200">Returns the updated event.</response>
    /// <response code="400">Invalid data, unknown venue or date moved into the past.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] EventRequestDto dto)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound(new ApiError("Event not found."));
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var error = await ValidateAsync(dto, ev.EventDate);
        if (error is not null) return BadRequest(new ApiError(error));

        ev.Name = dto.Name.Trim();
        ev.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        ev.EventDate = dto.EventDate;
        ev.VenueId = dto.VenueId;
        ev.EventType = dto.EventType;
        await _db.SaveChangesAsync();

        var updated = await EventQuery().AsNoTracking().FirstAsync(e => e.EventId == id);
        return Ok(updated.ToDto());
    }

    /// <summary>Deletes an event and its dependent categories, enrolments and results. Owner only.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound(new ApiError("Event not found."));
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> ValidateAsync(EventRequestDto dto, DateOnly? unchangedDate)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Name is required.";
        if (!Enum.IsDefined(typeof(EventType), dto.EventType))
            return "Event type must be Run, Walk or Cycle.";
        if (!await _db.Venues.AnyAsync(v => v.VenueId == dto.VenueId))
            return "The selected venue does not exist.";

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dto.EventDate < today && dto.EventDate != unchangedDate)
            return "The event date cannot be in the past.";

        return null;
    }
}