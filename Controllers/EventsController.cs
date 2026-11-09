using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>Public event pages and the Participant's enrol action.</summary>
public class EventsController : AppController
{
    private readonly IRaceDayApi _api;

    public EventsController(IRaceDayApi api) => _api = api;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.NotFound
                ? "We could not find that event."
                : result.Error ?? "We could not load that event.";
            return RedirectToAction("Index", "Home");
        }

        var isLoggedIn = User.Identity?.IsAuthenticated == true;
        var isParticipant = User.IsInRole("Participant");

        var model = new EventDetailsViewModel
        {
            Event = result.Data,
            IsLoggedIn = isLoggedIn,
            IsParticipant = isParticipant,
            IsUpcoming = result.Data.EventDate >= DateOnly.FromDateTime(DateTime.Today)
        };

        // Show "Enrolled" next to categories the participant has already entered.
        if (isParticipant)
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            if (mine.Unauthorized) return await SessionExpiredAsync();
            if (mine.Success && mine.Data is not null)
            {
                model.EnrolledCategoryIds = mine.Data
                    .Where(e => e.EventId == id && e.Status == "Confirmed")
                    .Select(e => e.CategoryId)
                    .ToHashSet();
            }
        }

        return View(model);
    }

    /// <summary>Enrols the logged-in Participant in a category. The API applies the rules (capacity, duplicates).</summary>
    [HttpPost]
    [Authorize(Roles = "Participant")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await _api.EnrolAsync(categoryId);
        if (result.Unauthorized) return await SessionExpiredAsync();

        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Error ?? "We could not complete your enrolment. Please try again.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = $"You are enrolled in {result.Data.EventName} ({result.Data.CategoryName}).";
        return RedirectToAction("Index", "Enrolments");
    }
}