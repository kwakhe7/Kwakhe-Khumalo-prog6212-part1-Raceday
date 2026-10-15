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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    private IQueryable<Result> ResultQuery() => _db.Results
        .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
        .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);

    /// <summary>Records the finishing result for an enrolment. Only the Organiser who owns the event may do this.</summary>
    /// <remarks>
    /// Expects finishTime (hh:mm:ss), position (1 or higher) and status (Finished, DNF or DQ).
    /// finishTime and position are required when status is Finished. The enrolment must be confirmed.
    /// </remarks>
    /// <response code="201">Result recorded. Returns the result including its ID.</response>
    /// <response code="400">Invalid data, or the enrolment is cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int enrolmentId, [FromBody] ResultRequestDto dto)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound(new ApiError("Enrolment not found."));
        if (enrolment.Category.Event.OrganiserId != User.GetUserId()) return Forbid();

        var error = Validate(dto);
        if (error is not null) return BadRequest(new ApiError(error));
        if (enrolment.Status != EnrolmentStatus.Confirmed)
            return BadRequest(new ApiError("Results can only be recorded for confirmed enrolments."));
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Conflict(new ApiError("A result already exists for this enrolment."));

        var result = new Result { EnrolmentId = enrolmentId };
        Apply(result, dto);
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var saved = await ResultQuery().AsNoTracking().FirstAsync(r => r.ResultId == result.ResultId);
        return Created($"/api/results/{result.ResultId}", saved.ToDto());
    }

    /// <summary>Corrects an existing result. Only the Organiser who owns the event may do this.</summary>
    /// <remarks>Expects finishTime, position and status, as when recording a result.</remarks>
    /// <response code="200">Returns the updated result.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ResultRequestDto dto)
    {
        var result = await ResultQuery().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result is null) return NotFound(new ApiError("Result not found."));
        if (result.Enrolment.Category.Event.OrganiserId != User.GetUserId()) return Forbid();

        var error = Validate(dto);
        if (error is not null) return BadRequest(new ApiError(error));

        Apply(result, dto);
        await _db.SaveChangesAsync();
        return Ok(result.ToDto());
    }

    /// <summary>Returns the results (leaderboard) of a category, ordered by finishing position. Public.</summary>
    /// <response code="200">Array of results; finishers first in position order, then DNF and DQ.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("categories/{categoryId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForCategory(int categoryId)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryId == categoryId))
            return NotFound(new ApiError("Category not found."));

        var results = await ResultQuery().AsNoTracking()
            .Where(r => r.Enrolment.CategoryId == categoryId)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .ToListAsync();
        return Ok(results.Select(r => r.ToDto()).ToList());
    }

    /// <summary>Lists the logged-in Participant's own results across all events. Participants only.</summary>
    /// <response code="200">Array of the participant's results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("users/me/results")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.GetUserId();
        var results = await ResultQuery().AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .ToListAsync();
        return Ok(results.Select(r => r.ToDto()).ToList());
    }

    private static string? Validate(ResultRequestDto dto)
    {
        if (!Enum.IsDefined(typeof(ResultStatus), dto.Status))
            return "Status must be Finished, DNF or DQ.";
        if (dto.Position is < 1)
            return "Position must be 1 or higher.";
        if (dto.FinishTime is { } time && time < TimeSpan.Zero)
            return "Finish time cannot be negative.";
        if (dto.Status == ResultStatus.Finished && (dto.FinishTime is null || dto.Position is null))
            return "Finish time and position are required when the status is Finished.";
        return null;
    }

    private static void Apply(Result result, ResultRequestDto dto)
    {
        result.Status = dto.Status;
        // DNF and DQ entrants have no finishing time or position.
        result.FinishTime = dto.Status == ResultStatus.Finished ? dto.FinishTime : null;
        result.Position = dto.Status == ResultStatus.Finished ? dto.Position : null;
    }
}