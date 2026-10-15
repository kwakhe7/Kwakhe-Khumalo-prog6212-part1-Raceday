using RaceDay.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Standard error body returned for 400, 401, 404 and 409 responses.</summary>
public record ApiError(string Message);

// ---------- Auth and users ----------

public class RegisterDto
{
    [Required, StringLength(100, MinimumLength = 1)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    /// <summary>Organiser or Participant.</summary>
    [Required] public UserRole? Role { get; set; }
}

public class LoginDto
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UpdateProfileDto
{
    [Required, StringLength(100, MinimumLength = 1)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
}

public class UserDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PublicUserDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public UserRole Role { get; set; }
}

// ---------- Venues, events, categories ----------

public class VenueDto
{
    public int VenueId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public int Capacity { get; set; }
}

public class EventRequestDto
{
    [Required, StringLength(150, MinimumLength = 1)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    /// <summary>Format yyyy-MM-dd. Cannot be in the past.</summary>
    public DateOnly EventDate { get; set; }
    [Range(1, int.MaxValue)] public int VenueId { get; set; }
    /// <summary>Run, Walk or Cycle.</summary>
    public EventType EventType { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public EventType EventType { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = "";
    public VenueDto Venue { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}

public class CategoryRequestDto
{
    [Required, StringLength(100, MinimumLength = 1)] public string Name { get; set; } = "";
    [Range(0.01, 999.99)] public decimal DistanceKm { get; set; }
    [Range(1, 100000)] public int MaxParticipants { get; set; } = 100;
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public int ConfirmedEnrolments { get; set; }
}

// ---------- Enrolments and results ----------

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public EnrolmentStatus Status { get; set; }
}

public class ResultRequestDto
{
    /// <summary>Finishing time (hh:mm:ss). Required when Status is Finished.</summary>
    public TimeSpan? FinishTime { get; set; }
    /// <summary>Finishing position (1 or higher). Required when Status is Finished.</summary>
    [Range(1, int.MaxValue)] public int? Position { get; set; }
    /// <summary>Finished, DNF or DQ.</summary>
    public ResultStatus Status { get; set; } = ResultStatus.Finished;
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public ResultStatus Status { get; set; }
}

// ---------- Entity to DTO mapping (PasswordHash is never mapped) ----------

public static class Mapper
{
    public static UserDto ToDto(this AppUser u) => new()
    {
        UserId = u.UserId,
        Name = u.Name,
        Email = u.Email,
        Role = u.Role,
        CreatedAt = u.CreatedAt
    };

    public static PublicUserDto ToPublicDto(this AppUser u) => new()
    {
        UserId = u.UserId,
        Name = u.Name,
        Role = u.Role
    };

    public static VenueDto ToDto(this Venue v) => new()
    {
        VenueId = v.VenueId,
        Name = v.Name,
        Address = v.Address,
        City = v.City,
        Capacity = v.Capacity
    };

    public static CategoryDto ToDto(this Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        Name = c.Name,
        DistanceKm = c.DistanceKm,
        MaxParticipants = c.MaxParticipants,
        ConfirmedEnrolments = c.Enrolments.Count(e => e.Status == EnrolmentStatus.Confirmed)
    };

    /// <summary>Requires Venue, Organiser and Categories (with Enrolments) to be loaded.</summary>
    public static EventDto ToDto(this Event e) => new()
    {
        EventId = e.EventId,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        EventType = e.EventType,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.Name,
        Venue = e.Venue.ToDto(),
        Categories = e.Categories.OrderBy(c => c.DistanceKm).Select(c => c.ToDto()).ToList()
    };

    /// <summary>Requires Participant and Category.Event to be loaded.</summary>
    public static EnrolmentDto ToDto(this Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant.Name,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.Name,
        EventId = e.Category.EventId,
        EventName = e.Category.Event.Name,
        EventDate = e.Category.Event.EventDate,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };

    /// <summary>Requires Enrolment.Participant and Enrolment.Category.Event to be loaded.</summary>
    public static ResultDto ToDto(this Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = r.Enrolment.Participant.Name,
        CategoryId = r.Enrolment.CategoryId,
        CategoryName = r.Enrolment.Category.Name,
        EventId = r.Enrolment.Category.EventId,
        EventName = r.Enrolment.Category.Event.Name,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status
    };
}