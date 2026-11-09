using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

public class HomeController : AppController
{
    private readonly IRaceDayApi _api;

    public HomeController(IRaceDayApi api) => _api = api;

    /// <summary>Upcoming events with filters. Public: anyone can browse.</summary>
    public async Task<IActionResult> Index([FromQuery] EventFilterViewModel filter)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Only upcoming events are listed: the start date is today unless the user picks a later one.
        var from = filter.From.HasValue && DateOnly.FromDateTime(filter.From.Value) > today
            ? DateOnly.FromDateTime(filter.From.Value)
            : today;

        var apiFilter = new EventFilter
        {
            From = from,
            To = filter.To.HasValue ? DateOnly.FromDateTime(filter.To.Value) : null,
            VenueId = filter.VenueId,
            EventType = string.IsNullOrWhiteSpace(filter.EventType) ? null : filter.EventType,
            Q = string.IsNullOrWhiteSpace(filter.Q) ? null : filter.Q.Trim()
        };

        var model = new HomeIndexViewModel { Filter = filter };

        var events = await _api.GetEventsAsync(apiFilter);
        if (events.Success && events.Data is not null)
            model.Events = events.Data;
        else
            model.Error = events.Error ?? "We could not load the events right now.";

        var venues = await _api.GetVenuesAsync();
        if (venues.Success && venues.Data is not null)
            model.Venues = venues.Data;

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}