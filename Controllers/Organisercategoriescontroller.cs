using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Models;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>Add, edit and delete the age or distance categories of one of the organiser's events.</summary>
[Authorize(Roles = "Organiser")]
[Route("Organiser/Events/{eventId:int}/Categories")]
public class OrganiserCategoriesController : OrganiserBaseController
{
    public OrganiserCategoriesController(IRaceDayApi api) : base(api) { }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await GetOwnEventAsync(eventId);
        if (failure is not null) return failure;

        return View(BuildPage(ev!, new CategoryFormViewModel()));
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventId, CategoryFormViewModel form)
    {
        var (ev, failure) = await GetOwnEventAsync(eventId);
        if (failure is not null) return failure;

        if (!ModelState.IsValid)
            return View("Index", BuildPage(ev!, form));

        var result = await Api.CreateCategoryAsync(eventId, ToRequest(form));
        if (result.Unauthorized) return await SessionExpiredAsync();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not add the category. Please try again.");
            return View("Index", BuildPage(ev!, form));
        }

        TempData["Success"] = $"Category \"{form.Name.Trim()}\" has been added.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpGet("{id:int}/Edit")]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var (ev, failure) = await GetOwnEventAsync(eventId);
        if (failure is not null) return failure;

        var category = ev!.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (category is null)
        {
            TempData["Error"] = "We could not find that category.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var form = new CategoryFormViewModel
        {
            CategoryId = id,
            Name = category.Name,
            DistanceKm = category.DistanceKm,
            MaxParticipants = category.MaxParticipants
        };
        return View(BuildEditPage(ev, form, eventId, id));
    }

    [HttpPost("{id:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel form)
    {
        var (ev, failure) = await GetOwnEventAsync(eventId);
        if (failure is not null) return failure;

        form.CategoryId = id;
        if (!ModelState.IsValid)
            return View(BuildEditPage(ev!, form, eventId, id));

        var result = await Api.UpdateCategoryAsync(id, ToRequest(form));
        if (result.Unauthorized) return await SessionExpiredAsync();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not save your changes. Please try again.");
            return View(BuildEditPage(ev!, form, eventId, id));
        }

        TempData["Success"] = "The category has been updated.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost("{id:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await Api.DeleteCategoryAsync(id);
        if (result.Unauthorized) return await SessionExpiredAsync();

        if (result.Success)
            TempData["Success"] = "The category has been deleted.";
        else
            TempData["Error"] = result.Error ?? "We could not delete that category.";

        return RedirectToAction(nameof(Index), new { eventId });
    }

    // ---------- helpers ----------

    private static CategoryRequest ToRequest(CategoryFormViewModel form) => new()
    {
        Name = form.Name.Trim(),
        DistanceKm = form.DistanceKm!.Value,
        MaxParticipants = form.MaxParticipants
    };

    private CategoriesPageViewModel BuildPage(EventDto ev, CategoryFormViewModel form)
    {
        form.FormAction = Url.Action(nameof(Create), new { eventId = ev.EventId });
        form.SubmitLabel = "Add category";
        return new CategoriesPageViewModel
        {
            EventId = ev.EventId,
            EventName = ev.Name,
            Categories = ev.Categories,
            NewCategory = form
        };
    }

    private CategoryEditPageViewModel BuildEditPage(EventDto ev, CategoryFormViewModel form, int eventId, int id)
    {
        form.FormAction = Url.Action(nameof(Edit), new { eventId, id });
        form.SubmitLabel = "Save changes";
        return new CategoryEditPageViewModel { EventId = ev.EventId, EventName = ev.Name, Form = form };
    }
}