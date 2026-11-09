using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>View and update the logged-in user's own details and profile picture (both roles).</summary>
[Authorize]
public class ProfileController : AppController
{
    private readonly IRaceDayApi _api;

    public ProfileController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var me = await _api.GetMeAsync();
        if (me.Unauthorized) return await SessionExpiredAsync();
        if (!me.Success || me.Data is null)
        {
            TempData["Error"] = me.Error ?? "We could not load your profile.";
            return RedirectToAction("Index", "Home");
        }

        return View(ToViewModel(me.Data));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDetails(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return await ReshowAsync(model);

        var result = await _api.UpdateMeAsync(new UpdateProfileRequest
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim()
        });
        if (result.Unauthorized) return await SessionExpiredAsync();

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not update your details. Please try again.");
            return await ReshowAsync(model);
        }

        await RefreshSignInAsync(result.Data);   // the menu shows the name from this login cookie
        TempData["Success"] = "Your details have been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image to upload.";
            return RedirectToAction(nameof(Index));
        }

        // The image goes to the API, which checks it and stores it in Azure Blob Storage.
        var result = await _api.UploadPictureAsync(picture);
        if (result.Unauthorized) return await SessionExpiredAsync();

        if (result.Success)
            TempData["Success"] = "Your profile picture has been updated.";
        else
            TempData["Error"] = result.Error ?? "We could not upload that picture. Please try again.";

        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------

    /// <summary>Shows the form again with the user's input, refilling the read-only details from the API.</summary>
    private async Task<IActionResult> ReshowAsync(ProfileViewModel model)
    {
        var me = await _api.GetMeAsync();
        if (me.Unauthorized) return await SessionExpiredAsync();
        if (!me.Success || me.Data is null)
        {
            TempData["Error"] = me.Error ?? "We could not load your profile.";
            return RedirectToAction("Index", "Home");
        }

        model.UserId = me.Data.UserId;
        model.Role = me.Data.Role;
        model.CreatedAt = me.Data.CreatedAt;
        model.HasProfilePicture = me.Data.HasProfilePicture;
        return View("Index", model);
    }

    private static ProfileViewModel ToViewModel(UserDto user) => new()
    {
        Name = user.Name,
        Email = user.Email,
        UserId = user.UserId,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
        HasProfilePicture = user.HasProfilePicture
    };

    private async Task RefreshSignInAsync(UserDto user)
    {
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
    }
}