using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>Create, edit and delete the Organiser's own events, including the banner image.</summary>
[Authorize(Roles = "Organiser")]
[Route("Organiser/Events")]
public class OrganiserEventsController : AppController
{
    private readonly IRaceDayApi _api;

    public OrganiserEventsController(IRaceDayApi api) => _api = api;

    // ---------- List ----------

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEventsAsync();
        if (result.Unauthorized) return await SessionExpiredAsync();

        var model = new ManageEventsViewModel();
        if (result.Success && result.Data is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            model.Upcoming = result.Data.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToList();
            model.Past = result.Data.Where(e => e.EventDate < today).OrderByDescending(e => e.EventDate).ToList();
        }
        else
        {
            TempData["Error"] = result.Error ?? "We could not load your events.";
        }

        return View(model);
    }

    // ---------- Create ----------

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var model = new EventFormViewModel { EventDate = DateTime.Today.AddDays(30) };
        if (!await FillVenuesAsync(model))
        {
            TempData["Error"] = "We could not load the venues. Please try again.";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        model.IsEdit = false;
        model.Id = null;

        // The event's distance is kept on its categories, so the first one is created together with the event.
        if (!model.DistanceKm.HasValue)
            ModelState.AddModelError(nameof(model.DistanceKm), "Please enter the distance of the event.");

        if (!ModelState.IsValid)
            return await ReshowAsync(model, "Create");

        var created = await _api.CreateEventAsync(ToRequest(model));
        if (created.Unauthorized) return await SessionExpiredAsync();
        if (!created.Success || created.Data is null)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "We could not create the event. Please try again.");
            return await ReshowAsync(model, "Create");
        }

        var eventId = created.Data.EventId;
        var warnings = new List<string>();

        var categoryName = string.IsNullOrWhiteSpace(model.CategoryName)
            ? $"{model.DistanceKm!.Value:0.##} km"
            : model.CategoryName.Trim();
        var category = await _api.CreateCategoryAsync(eventId, new CategoryRequest
        {
            Name = categoryName,
            DistanceKm = model.DistanceKm!.Value,
            MaxParticipants = model.MaxParticipants
        });
        if (!category.Success)
            warnings.Add($"The event was created, but its first category was not: {category.Error}");

        if (model.Banner is { Length: > 0 })
        {
            var upload = await _api.UploadBannerAsync(eventId, model.Banner);
            if (!upload.Success)
                warnings.Add($"The event was created, but the banner was not uploaded: {upload.Error}");
        }

        TempData["Success"] = $"\"{created.Data.Name}\" has been created.";
        if (warnings.Count > 0) TempData["Info"] = string.Join(" ", warnings);
        return RedirectToAction(nameof(Index));
    }

    // ---------- Edit ----------

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        // The API only returns this organiser's own events here, so another organiser's event cannot be opened.
        var mine = await _api.GetMyEventsAsync();
        if (mine.Unauthorized) return await SessionExpiredAsync();

        var ev = mine.Data?.FirstOrDefault(e => e.EventId == id);
        if (!mine.Success || ev is null)
        {
            TempData["Error"] = mine.Success ? "We could not find that event among your events." : mine.Error;
            return RedirectToAction(nameof(Index));
        }

        var model = new EventFormViewModel
        {
            IsEdit = true,
            Id = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate.ToDateTime(TimeOnly.MinValue),
            VenueId = ev.Venue.VenueId,
            EventType = ev.EventType,
            HasBanner = ev.HasBanner,
            Categories = ev.Categories
        };
        await FillVenuesAsync(model);
        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.IsEdit = true;
        model.Id = id;

        // The first-category fields only apply when creating an event.
        ModelState.Remove(nameof(model.DistanceKm));
        ModelState.Remove(nameof(model.CategoryName));
        ModelState.Remove(nameof(model.MaxParticipants));

        if (!ModelState.IsValid)
            return await ReshowEditAsync(id, model);

        var updated = await _api.UpdateEventAsync(id, ToRequest(model));
        if (updated.Unauthorized) return await SessionExpiredAsync();
        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error ?? "We could not save your changes. Please try again.");
            return await ReshowEditAsync(id, model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var upload = await _api.UploadBannerAsync(id, model.Banner);
            if (!upload.Success)
            {
                TempData["Success"] = "Your changes were saved.";
                TempData["Info"] = $"The banner was not updated: {upload.Error}";
                return RedirectToAction(nameof(Index));
            }
        }

        TempData["Success"] = "Your changes were saved.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Delete ----------

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteEventAsync(id);
        if (result.Unauthorized) return await SessionExpiredAsync();

        if (result.Success)
            TempData["Success"] = "The event has been deleted.";
        else
            TempData["Error"] = result.Error ?? "We could not delete that event.";

        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------

    private static EventRequest ToRequest(EventFormViewModel model) => new()
    {
        Name = model.Name.Trim(),
        Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
        EventDate = DateOnly.FromDateTime(model.EventDate!.Value),
        VenueId = model.VenueId!.Value,
        EventType = model.EventType
    };

    private async Task<bool> FillVenuesAsync(EventFormViewModel model)
    {
        var venues = await _api.GetVenuesAsync();
        if (!venues.Success || venues.Data is null) return false;
        model.Venues = venues.Data;
        return true;
    }

    private async Task<IActionResult> ReshowAsync(EventFormViewModel model, string viewName)
    {
        await FillVenuesAsync(model);
        return View(viewName, model);
    }

    /// <summary>Shows the edit form again, refilling the read-only parts (venues, categories, banner flag) from the API.</summary>
    private async Task<IActionResult> ReshowEditAsync(int id, EventFormViewModel model)
    {
        await FillVenuesAsync(model);

        var current = await _api.GetEventAsync(id);
        if (current.Unauthorized) return await SessionExpiredAsync();
        if (current.Success && current.Data is not null)
        {
            model.Categories = current.Data.Categories;
            model.HasBanner = current.Data.HasBanner;
        }
        return View("Edit", model);
    }
}