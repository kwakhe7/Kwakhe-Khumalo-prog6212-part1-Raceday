using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>The Organiser's dashboard.</summary>
[Authorize(Roles = "Organiser")]
public class OrganiserController : AppController
{
    private readonly IRaceDayApi _api;

    public OrganiserController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEventsAsync();
        if (result.Unauthorized) return await SessionExpiredAsync();

        var model = new OrganiserDashboardViewModel();
        if (!result.Success || result.Data is null)
        {
            model.Error = result.Error ?? "We could not load your events right now.";
            return View(model);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        model.Upcoming = result.Data.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToList();
        model.Past = result.Data.Where(e => e.EventDate < today).OrderByDescending(e => e.EventDate).ToList();
        model.TotalEvents = result.Data.Count;
        model.UpcomingCount = model.Upcoming.Count;
        model.TotalEnrolments = result.Data.Sum(e => e.TotalEnrolments);
        model.NextEvent = model.Upcoming.FirstOrDefault();

        return View(model);
    }
}