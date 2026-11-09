using System.ComponentModel.DataAnnotations;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Models;

/// <summary>The filters on the home page. They arrive in the query string, so a filtered page can be bookmarked.</summary>
public class EventFilterViewModel
{
    public string? Q { get; set; }
    public string? EventType { get; set; }
    public int? VenueId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    public DateTime? To { get; set; }
}

public class HomeIndexViewModel
{
    public EventFilterViewModel Filter { get; set; } = new();
    public List<EventDto> Events { get; set; } = new();
    public List<VenueDto> Venues { get; set; } = new();
    /// <summary>A friendly message when the events could not be loaded.</summary>
    public string? Error { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    /// <summary>Categories of this event the logged-in participant is already enrolled in.</summary>
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
    public bool IsLoggedIn { get; set; }
    public bool IsParticipant { get; set; }
    public bool IsUpcoming { get; set; }
}

public class MyEnrolmentsViewModel
{
    public List<EnrolmentDto> Upcoming { get; set; } = new();
    public List<EnrolmentDto> Past { get; set; } = new();
}
