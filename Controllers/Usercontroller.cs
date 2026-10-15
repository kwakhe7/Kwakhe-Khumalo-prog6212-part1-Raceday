using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db) => _db = db;

    /// <summary>Returns the profile of the logged-in user.</summary>
    /// <response code="200">The user's ID, name, email, role and creation date (never the password).</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        return user is null ? NotFound(new ApiError("User not found.")) : Ok(user.ToDto());
    }

    /// <summary>Updates the logged-in user's own name and email.</summary>
    /// <remarks>Expects name and email. The role cannot be changed.</remarks>
    /// <response code="200">Returns the updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="409">The email belongs to another account.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new ApiError("Name and email are required."));

        var userId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null) return NotFound(new ApiError("User not found."));

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != userId))
            return Conflict(new ApiError("This email is already used by another account."));

        user.Name = dto.Name.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();
        return Ok(user.ToDto());
    }

    /// <summary>Returns another user's public profile (ID, name and role only).</summary>
    /// <response code="200">The public profile.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PublicUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        return user is null ? NotFound(new ApiError("User not found.")) : Ok(user.ToPublicDto());
    }
}
