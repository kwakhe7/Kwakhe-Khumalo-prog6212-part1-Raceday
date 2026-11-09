using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>The Participant's personal race history.</summary>
[Authorize(Roles = "Participant")]
public class ResultsController : AppController
{
    private readonly IRaceDayApi _api;

    public ResultsController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var results = await _api.GetMyResultsAsync();
        if (results.Unauthorized) return await SessionExpiredAsync();

        var model = new MyResultsViewModel();
        if (!results.Success || results.Data is null)
        {
            TempData["Error"] = results.Error ?? "We could not load your results.";
            return View(model);
        }

        // The result does not carry the event date, but the participant's enrolments do.
        var dates = new Dictionary<int, DateOnly>();
        var enrolments = await _api.GetMyEnrolmentsAsync();
        if (enrolments.Success && enrolments.Data is not null)
            dates = enrolments.Data.ToDictionary(e => e.EnrolmentId, e => e.EventDate);

        model.Rows = results.Data
            .Select(r => new ResultRowViewModel
            {
                Result = r,
                EventDate = dates.TryGetValue(r.EnrolmentId, out var date) ? date : null
            })
            .OrderByDescending(row => row.EventDate)
            .ToList();

        var finished = model.Rows.Where(r => r.Result.Status == "Finished").ToList();
        model.RacesFinished = finished.Count;
        model.BestPosition = finished.Where(r => r.Result.Position.HasValue).Select(r => r.Result.Position).Min();

        return View(model);
    }
}