using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

public class AccountController : AppController
{
    private readonly IRaceDayApi _api;

    public AccountController(IRaceDayApi api) => _api = api;

    // ---------- Register ----------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToLanding(User.FindFirst(ClaimTypes.Role)?.Value);
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role is not ("Organiser" or "Participant"))
            ModelState.AddModelError(nameof(model.Role), "Please choose a role.");

        if (!ModelState.IsValid)
            return ReturnWithoutPasswords(model);

        var result = await _api.RegisterAsync(new RegisterRequest
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            Role = model.Role
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not create your account. Please try again.");
            return ReturnWithoutPasswords(model);
        }

        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Login ----------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToLanding(User.FindFirst(ClaimTypes.Role)?.Value);
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            model.Password = "";
            return View(model);
        }

        var result = await _api.LoginAsync(new LoginRequest { Email = model.Email.Trim(), Password = model.Password });

        // The API's session cookie is captured by ApiCookieHandler. Without it, later calls would fail.
        var hasApiSession = !string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeys.ApiCookie));

        if (!result.Success || result.Data is null || !hasApiSession)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not log you in. Please try again.");
            model.Password = "";
            return View(model);
        }

        var user = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToLanding(user.Role);
    }

    // ---------- Logout ----------

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();   // ends the API session; errors do not matter here
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Other ----------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    /// <summary>Organisers land on their dashboard; everyone else on the events page.</summary>
    private IActionResult RedirectToLanding(string? role) =>
        role == "Organiser"
            ? RedirectToAction("Index", "Organiser")
            : RedirectToAction("Index", "Home");

    private IActionResult ReturnWithoutPasswords(RegisterViewModel model)
    {
        model.Password = "";
        model.ConfirmPassword = "";
        return View(model);
    }
}