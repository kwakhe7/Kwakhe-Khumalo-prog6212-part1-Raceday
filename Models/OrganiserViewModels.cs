using System.ComponentModel.DataAnnotations;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Models;

public class OrganiserDashboardViewModel
{
    public List<EventDto> Upcoming { get; set; } = new();
    public List<EventDto> Past { get; set; } = new();
    public int TotalEvents { get; set; }
    public int UpcomingCount { get; set; }
    public int TotalEnrolments { get; set; }
    public EventDto? NextEvent { get; set; }
    public string? Error { get; set; }
}

public class ManageEventsViewModel
{
    public List<EventDto> Upcoming { get; set; } = new();
    public List<EventDto> Past { get; set; } = new();
}

/// <summary>The form used to create and to edit an event.</summary>
public class EventFormViewModel
{
    public bool IsEdit { get; set; }
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150, ErrorMessage = "The name must be 150 characters or fewer.")]
    [Display(Name = "Event name")]
    public string Name { get; set; } = "";

    [StringLength(500, ErrorMessage = "The description must be 500 characters or fewer.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please choose a venue.")]
    [Display(Name = "Venue (location)")]
    public int? VenueId { get; set; }

    [Required(ErrorMessage = "Please choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    // ----- Used when creating: the first category carries the event's distance -----

    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [StringLength(100, ErrorMessage = "The category name must be 100 characters or fewer.")]
    [Display(Name = "Category name")]
    public string? CategoryName { get; set; }

    [Range(1, 100000, ErrorMessage = "Please enter a number of places between 1 and 100000.")]
    [Display(Name = "Places available")]
    public int MaxParticipants { get; set; } = 100;

    // ----- Banner image -----
    public IFormFile? Banner { get; set; }
    public bool HasBanner { get; set; }

    // ----- Filled by the controller for display only -----
    public List<VenueDto> Venues { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}