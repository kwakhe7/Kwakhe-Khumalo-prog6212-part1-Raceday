using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>The Participant's own enrolments.</summary>
[Authorize(Roles = "Participant")]
public class EnrolmentsController : AppController
{
    private readonly IRaceDayApi _api;

    public EnrolmentsController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        if (result.Unauthorized) return await SessionExpiredAsync();

        var model = new MyEnrolmentsViewModel();
        if (result.Success && result.Data is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            model.Upcoming = result.Data.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToList();
            model.Past = result.Data.Where(e => e.EventDate < today).OrderByDescending(e => e.EventDate).ToList();
        }
        else
        {
            TempData["Error"] = result.Error ?? "We could not load your enrolments.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _api.CancelEnrolmentAsync(id);
        if (result.Unauthorized) return await SessionExpiredAsync();

        if (result.Success)
            TempData["Success"] = "Your enrolment has been cancelled.";
        else
            TempData["Error"] = result.Error ?? "We could not cancel that enrolment.";

        return RedirectToAction(nameof(Index));
    }
}