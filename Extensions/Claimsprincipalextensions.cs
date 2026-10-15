using System.Security.Claims;

namespace RaceDay.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the logged-in user's ID from the session claims (never from the request body).</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The session does not contain a user ID.");
    }
}