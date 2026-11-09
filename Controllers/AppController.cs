using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace RACEDAY_P3.Controllers;

/// <summary>Base class for the app's controllers, with shared session handling.</summary>
public abstract class AppController : Controller
{
    /// <summary>The API said the session is no longer valid: end the MVC session and ask the user to log in again.</summary>
    protected async Task<IActionResult> SessionExpiredAsync()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Info"] = "Your session has expired. Please log in again.";
        return RedirectToAction("Login", "Account");
    }
}