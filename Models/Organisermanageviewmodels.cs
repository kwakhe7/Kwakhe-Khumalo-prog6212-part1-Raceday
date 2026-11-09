using System.ComponentModel.DataAnnotations;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Models;

/// <summary>Data for the tab strip shown at the top of the Categories, Enrolments and Results pages.</summary>
public record EventTabsModel(int EventId, string EventName, string Active);

// ---------- Categories ----------

public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter the category name.")]
    [StringLength(100, ErrorMessage = "The name must be 100 characters or fewer.")]
    [Display(Name = "Category name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(1, 100000, ErrorMessage = "Please enter a number of places between 1 and 100000.")]
    [Display(Name = "Places available")]
    public int MaxParticipants { get; set; } = 100;

    // Set by the controller for display only (not posted by the form)
    public string? FormAction { get; set; }
    public string SubmitLabel { get; set; } = "Save";
}

public class CategoriesPageViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class CategoryEditPageViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public CategoryFormViewModel Form { get; set; } = new();
}

// ---------- Enrolments ----------

public class CategoryEnrolmentsGroup
{
    public CategoryDto Category { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class EventEnrolmentsViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public List<CategoryEnrolmentsGroup> Groups { get; set; } = new();
    public int ConfirmedCount { get; set; }
    public string? Error { get; set; }
}

// ---------- Results entry ----------

public class ResultEntryRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ResultsEntryGroup
{
    public CategoryDto Category { get; set; } = new();
    public List<ResultEntryRow> Rows { get; set; } = new();
}

public class ResultsEntryPageViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    /// <summary>Results can be entered once the event date has arrived.</summary>
    public bool EventHasStarted { get; set; }
    public List<ResultsEntryGroup> Groups { get; set; } = new();
}

/// <summary>What one row of the results form posts.</summary>
public class ResultEntryInput
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}