using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    private IQueryable<Enrolment> EnrolmentQuery() => _db.Enrolments
        .Include(e => e.Participant)
        .Include(e => e.Category).ThenInclude(c => c.Event);

    /// <summary>Enrols the logged-in Participant in a category. Participants only.</summary>
    /// <remarks>
    /// No body is needed: the participant is identified from the session. The category must have space,
    /// and a participant can only hold one active enrolment per category. A previously cancelled
    /// enrolment is re-activated.
    /// </remarks>
    /// <response code="201">Enrolled. Returns the enrolment including its ID.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category is full, or the participant is already enrolled.</response>
    [HttpPost("categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound(new ApiError("Category not found."));

        var userId = User.GetUserId();
        var existing = await _db.Enrolments.FirstOrDefaultAsync(e => e.ParticipantId == userId && e.CategoryId == categoryId);
        if (existing is not null && existing.Status == EnrolmentStatus.Confirmed)
            return Conflict(new ApiError("You are already enrolled in this category."));

        var confirmed = await _db.Enrolments.CountAsync(e => e.CategoryId == categoryId && e.Status == EnrolmentStatus.Confirmed);
        if (confirmed >= category.MaxParticipants)
            return Conflict(new ApiError("This category is full."));

        Enrolment enrolment;
        if (existing is not null)
        {
            existing.Status = EnrolmentStatus.Confirmed;
            existing.EnrolmentDate = DateTime.Now;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment
            {
                ParticipantId = userId,
                CategoryId = categoryId,
                EnrolmentDate = DateTime.Now,
                Status = EnrolmentStatus.Confirmed
            };
            _db.Enrolments.Add(enrolment);
        }
        await _db.SaveChangesAsync();

        var saved = await EnrolmentQuery().AsNoTracking().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return Created($"/api/enrolments/{enrolment.EnrolmentId}", saved.ToDto());
    }

    /// <summary>Lists all enrolments made by the logged-in Participant. Participants only.</summary>
    /// <response code="200">Array of the participant's enrolments with event and category details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("users/me/enrolments")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.GetUserId();
        var list = await EnrolmentQuery().AsNoTracking()
            .Where(e => e.ParticipantId == userId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();
        return Ok(list.Select(e => e.ToDto()).ToList());
    }

    /// <summary>Lists everyone enrolled in a category. Only the Organiser who owns the event may do this.</summary>
    /// <response code="200">Array of enrolments (confirmed and cancelled).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForCategory(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound(new ApiError("Category not found."));
        if (category.Event.OrganiserId != User.GetUserId()) return Forbid();

        var list = await EnrolmentQuery().AsNoTracking()
            .Where(e => e.CategoryId == categoryId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();
        return Ok(list.Select(e => e.ToDto()).ToList());
    }

    /// <summary>Cancels an enrolment. Callable by the enrolled Participant or the Organiser who owns the event.</summary>
    /// <remarks>The enrolment is kept with status Cancelled, which frees the place. It cannot be cancelled once a result exists.</remarks>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Neither the enrolled participant nor the owning organiser.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been recorded for this enrolment.</response>
    [HttpDelete("enrolments/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound(new ApiError("Enrolment not found."));

        var userId = User.GetUserId();
        var isEnrolledParticipant = enrolment.ParticipantId == userId;
        var isOwningOrganiser = User.IsInRole(AppRoles.Organiser) && enrolment.Category.Event.OrganiserId == userId;
        if (!isEnrolledParticipant && !isOwningOrganiser) return Forbid();

        if (enrolment.Result is not null)
            return Conflict(new ApiError("A result has already been recorded for this enrolment."));

        enrolment.Status = EnrolmentStatus.Cancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
