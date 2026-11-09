using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>Everyone who has enrolled in one of the organiser's events, grouped by category.</summary>
[Authorize(Roles = "Organiser")]
[Route("Organiser/Events/{eventId:int}/Enrolments")]
public class OrganiserEnrolmentsController : OrganiserBaseController
{
    public OrganiserEnrolmentsController(IRaceDayApi api) : base(api) { }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await GetOwnEventAsync(eventId);
        if (failure is not null) return failure;

        var model = new EventEnrolmentsViewModel { EventId = ev!.EventId, EventName = ev.Name };
        var problems = 0;

        foreach (var category in ev.Categories)
        {
            var result = await Api.GetCategoryEnrolmentsAsync(category.CategoryId);
            if (result.Unauthorized) return await SessionExpiredAsync();

            if (!result.Success || result.Data is null)
            {
                problems++;
                model.Groups.Add(new CategoryEnrolmentsGroup { Category = category });
                continue;
            }

            model.Groups.Add(new CategoryEnrolmentsGroup
            {
                Category = category,
                Enrolments = result.Data.OrderBy(e => e.ParticipantName).ToList()
            });
        }

        model.ConfirmedCount = model.Groups.Sum(g => g.Enrolments.Count(e => e.Status == "Confirmed"));
        if (problems > 0)
            model.Error = "Some enrolments could not be loaded. Please refresh the page to try again.";

        return View(model);
    }
}
