using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string InvalidCredentials = "Invalid email or password.";

    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;

    public AuthController(RaceDayDbContext db, IPasswordHasher<AppUser> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>Registers a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Expects name, email, password (minimum 8 characters) and role (Organiser or Participant).
    /// The password is hashed before it is stored. Only the two roles above are accepted.
    /// </remarks>
    /// <response code="201">Account created. Returns the new user's ID, name, email and role.</response>
    /// <response code="400">Invalid or missing data, or an unknown role.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto.Role is null || !Enum.IsDefined(typeof(UserRole), dto.Role.Value))
            return BadRequest(new ApiError("Role must be 'Organiser' or 'Participant'."));

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new ApiError("An account with this email already exists."));

        var user = new AppUser
        {
            Name = dto.Name.Trim(),
            Email = email,
            Role = dto.Role.Value,
            CreatedAt = DateTime.Now
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction("GetById", "Users", new { id = user.UserId }, user.ToDto());
    }

    /// <summary>Logs in and starts a session.</summary>
    /// <remarks>
    /// Expects email and password. On success the server sets a session cookie
    /// (RaceDay.Session) that identifies the user and their role on every later request.
    /// </remarks>
    /// <response code="200">Logged in. Returns the user's ID, name, email and role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
            return Unauthorized(new ApiError(InvalidCredentials));

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
            return Unauthorized(new ApiError(InvalidCredentials));

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            await _db.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(user.ToDto());
    }

    /// <summary>Logs out and ends the current session.</summary>
    /// <response code="204">Session ended.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}