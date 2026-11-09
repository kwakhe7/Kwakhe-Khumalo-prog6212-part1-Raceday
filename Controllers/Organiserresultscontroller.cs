using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>Capture finish times and positions for the people enrolled in one of the organiser's events.</summary>
[Authorize(Roles = "Organiser")]
[Route("Organiser/Events/{eventId:int}/Results")]
public class OrganiserResultsController : OrganiserBaseController
{
    private static readonly string[] TimeFormats = { @"h\:mm\:ss", @"hh\:mm\:ss" };
    private static readonly string[] AllowedStatuses = { "Finished", "DNF", "DQ" };

    public OrganiserResultsController(IRaceDayApi api) : base(api) { }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await GetOwnEventAsync(eventId);
        if (failure is not null) return failure;

        var model = new ResultsEntryPageViewModel
        {
            EventId = ev!.EventId,
            EventName = ev.Name,
            EventDate = ev.EventDate,
            EventHasStarted = ev.EventDate <= DateOnly.FromDateTime(DateTime.Today)
        };

        foreach (var category in ev.Categories)
        {
            var enrolments = await Api.GetCategoryEnrolmentsAsync(category.CategoryId);
            if (enrolments.Unauthorized) return await SessionExpiredAsync();

            var results = await Api.GetCategoryResultsAsync(category.CategoryId);
            var byEnrolment = results.Success && results.Data is not null
                ? results.Data.ToDictionary(r => r.EnrolmentId)
                : new Dictionary<int, ResultDto>();

            var rows = new List<ResultEntryRow>();
            if (enrolments.Success && enrolments.Data is not null)
            {
                rows = enrolments.Data
                    .Where(e => e.Status == "Confirmed")
                    .OrderBy(e => e.ParticipantName)
                    .Select(e => new ResultEntryRow
                    {
                        Enrolment = e,
                        Result = byEnrolment.TryGetValue(e.EnrolmentId, out var result) ? result : null
                    })
                    .ToList();
            }
            else
            {
                TempData["Error"] = "Some participants could not be loaded. Please refresh the page.";
            }

            model.Groups.Add(new ResultsEntryGroup { Category = category, Rows = rows });
        }

        return View(model);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int eventId, ResultEntryInput input)
    {
        if (!AllowedStatuses.Contains(input.Status))
        {
            TempData["Error"] = "Please choose Finished, DNF or DQ.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        TimeSpan? finishTime = null;
        if (input.Status == "Finished")
        {
            if (string.IsNullOrWhiteSpace(input.FinishTime) || !input.Position.HasValue)
            {
                TempData["Error"] = "Please enter both the finish time and the position for a finisher.";
                return RedirectToAction(nameof(Index), new { eventId });
            }

            if (!TimeSpan.TryParseExact(input.FinishTime.Trim(), TimeFormats, CultureInfo.InvariantCulture, out var parsed))
            {
                TempData["Error"] = "Please enter the finish time as hours:minutes:seconds, for example 00:45:12.";
                return RedirectToAction(nameof(Index), new { eventId });
            }
            finishTime = parsed;
        }

        var request = new ResultRequest
        {
            Status = input.Status,
            FinishTime = finishTime,
            Position = input.Status == "Finished" ? input.Position : null
        };

        // The API applies the rules (for example, results only after the event date).
        var result = input.ResultId.HasValue
            ? await Api.UpdateResultAsync(input.ResultId.Value, request)
            : await Api.RecordResultAsync(input.EnrolmentId, request);

        if (result.Unauthorized) return await SessionExpiredAsync();

        if (result.Success)
            TempData["Success"] = "Result saved. It is now visible to the participant.";
        else
            TempData["Error"] = result.Error ?? "We could not save that result. Please try again.";

        return RedirectToAction(nameof(Index), new { eventId });
    }
}