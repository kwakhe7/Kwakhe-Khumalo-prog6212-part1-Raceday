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
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public CategoriesController(RaceDayDbContext db) => _db = db;

        /// <summary>Lists all categories (for example age groups or distances) of an event. Public.</summary>
        /// <response code="200">Array of categories with their distance, capacity and confirmed enrolments.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("events/{eventId:int}/categories")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
                return NotFound(new ApiError("Event not found."));

            var categories = await _db.Categories.AsNoTracking()
                .Include(c => c.Enrolments)
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.DistanceKm)
                .ToListAsync();
            return Ok(categories.Select(c => c.ToDto()).ToList());
        }

        /// <summary>Adds a category to an event. Only the Organiser who owns the event may do this.</summary>
        /// <remarks>Expects name, distanceKm (0.01 to 999.99) and maxParticipants (1 or more).</remarks>
        /// <response code="201">Category created. Returns the category including its new ID.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the event.</response>
        /// <response code="404">Event not found.</response>
        [HttpPost("events/{eventId:int}/categories")]
        [Authorize(Roles = AppRoles.Organiser)]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(int eventId, [FromBody] CategoryRequestDto dto)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev is null) return NotFound(new ApiError("Event not found."));
            if (ev.OrganiserId != User.GetUserId()) return Forbid();

            var error = Validate(dto);
            if (error is not null) return BadRequest(new ApiError(error));

            var category = new Category
            {
                EventId = eventId,
                Name = dto.Name.Trim(),
                DistanceKm = dto.DistanceKm,
                MaxParticipants = dto.MaxParticipants
            };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return Created($"/api/events/{eventId}/categories", category.ToDto());
        }

        /// <summary>Updates a category. Only the Organiser who owns the parent event may do this.</summary>
        /// <remarks>Expects name, distanceKm and maxParticipants. Capacity cannot go below the current confirmed enrolments.</remarks>
        /// <response code="200">Returns the updated category.</response>
        /// <response code="400">Invalid data, or capacity lower than the confirmed enrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the parent event.</response>
        /// <response code="404">Category not found.</response>
        [HttpPut("categories/{id:int}")]
        [Authorize(Roles = AppRoles.Organiser)]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequestDto dto)
        {
            var category = await _db.Categories
                .Include(c => c.Event)
                .Include(c => c.Enrolments)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category is null) return NotFound(new ApiError("Category not found."));
            if (category.Event.OrganiserId != User.GetUserId()) return Forbid();

            var error = Validate(dto);
            if (error is not null) return BadRequest(new ApiError(error));

            var confirmed = category.Enrolments.Count(e => e.Status == EnrolmentStatus.Confirmed);
            if (dto.MaxParticipants < confirmed)
                return BadRequest(new ApiError($"Capacity cannot be lower than the {confirmed} confirmed enrolments."));

            category.Name = dto.Name.Trim();
            category.DistanceKm = dto.DistanceKm;
            category.MaxParticipants = dto.MaxParticipants;
            await _db.SaveChangesAsync();

            return Ok(category.ToDto());
        }

        /// <summary>Deletes a category, provided it has no confirmed enrolments. Owner only.</summary>
        /// <response code="204">Category deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or an Organiser who does not own the parent event.</response>
        /// <response code="404">Category not found.</response>
        /// <response code="409">The category still has confirmed enrolments.</response>
        [HttpDelete("categories/{id:int}")]
        [Authorize(Roles = AppRoles.Organiser)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Event)
                .Include(c => c.Enrolments)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category is null) return NotFound(new ApiError("Category not found."));
            if (category.Event.OrganiserId != User.GetUserId()) return Forbid();

            if (category.Enrolments.Any(e => e.Status == EnrolmentStatus.Confirmed))
                return Conflict(new ApiError("This category has confirmed enrolments and cannot be deleted."));

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static string? Validate(CategoryRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return "Name is required.";
            if (dto.DistanceKm <= 0 || dto.DistanceKm > 999.99m) return "Distance must be between 0.01 and 999.99 km.";
            if (dto.MaxParticipants < 1) return "Maximum participants must be at least 1.";
            return null;
        }
    }
