using Microsoft.AspNetCore.Http;

namespace RACEDAY_P3.Services;

public static class SessionKeys
{
    public const string ApiCookie = "ApiCookie";
}

/// <summary>
/// Carries the user's API session. After login the API sends back a session cookie. This handler keeps it
/// in the MVC session (server side) and attaches it to every later API call made for that user.
/// </summary>
public class ApiCookieHandler : DelegatingHandler
{
    // Must match the cookie name configured in the API (Part 2).
    public const string ApiCookieName = "RaceDay.Session";

    private readonly IHttpContextAccessor _accessor;

    public ApiCookieHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _accessor.HttpContext?.Session;

        var stored = session?.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(stored))
            request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={stored}");

        var response = await base.SendAsync(request, cancellationToken);

        // Keep the newest cookie (set at login, and refreshed by the API's sliding expiry).
        if (session is not null && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var header in setCookies)
            {
                if (!header.StartsWith(ApiCookieName + "=", StringComparison.Ordinal)) continue;
                var value = header.Substring(ApiCookieName.Length + 1).Split(';')[0];
                if (!string.IsNullOrEmpty(value))   // an empty value means the API ended the session (logout)
                    session.SetString(SessionKeys.ApiCookie, value);
            }
        }

        return response;
    }
}