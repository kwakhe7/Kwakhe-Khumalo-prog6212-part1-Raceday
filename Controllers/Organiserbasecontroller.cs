using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>Shared by the Organiser's per-event pages (categories, enrolments, results).</summary>
public abstract class OrganiserBaseController : AppController
{
    protected readonly IRaceDayApi Api;

    protected OrganiserBaseController(IRaceDayApi api) => Api = api;

    /// <summary>
    /// Finds one of this organiser's own events. The API only returns the logged-in organiser's events here,
    /// so another organiser's event can never be opened. Returns a redirect when the event cannot be used.
    /// </summary>
    protected async Task<(EventDto? Event, IActionResult? Failure)> GetOwnEventAsync(int eventId)
    {
        var mine = await Api.GetMyEventsAsync();
        if (mine.Unauthorized) return (null, await SessionExpiredAsync());

        var ev = mine.Data?.FirstOrDefault(e => e.EventId == eventId);
        if (!mine.Success || ev is null)
        {
            TempData["Error"] = mine.Success
                ? "We could not find that event among your events."
                : mine.Error ?? "We could not load your events.";
            return (null, RedirectToAction("Index", "OrganiserEvents"));
        }

        return (ev, null);
    }
}