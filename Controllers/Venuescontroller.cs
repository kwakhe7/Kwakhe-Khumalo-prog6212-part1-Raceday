using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/venues")]
[Authorize]
public class VenuesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public VenuesController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists all venues so an Organiser can pick a venueId when creating an event.</summary>
    /// <remarks>Supporting endpoint added in Part 2 (not in the Part 1 plan). Read-only.</remarks>
    /// <response code="200">Array of venues.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<VenueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var venues = await _db.Venues.AsNoTracking().OrderBy(v => v.Name).ToListAsync();
        return Ok(venues.Select(v => v.ToDto()).ToList());
    }
}
