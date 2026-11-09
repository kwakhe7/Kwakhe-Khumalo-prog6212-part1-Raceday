namespace RACEDAY_P3.Services;

// These classes mirror the JSON the Part 2 API sends and receives.
// Enum values (role, event type, status) arrive as text, for example "Organiser" or "Confirmed".

public class UserDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool HasProfilePicture { get; set; }
}

public class VenueDto
{
    public int VenueId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public int Capacity { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public int ConfirmedEnrolments { get; set; }
    public int PlacesLeft => Math.Max(0, MaxParticipants - ConfirmedEnrolments);
}

public class EventDto
{
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string EventType { get; set; } = "";
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = "";
    public VenueDto Venue { get; set; } = new();
    public bool HasBanner { get; set; }
    public int TotalEnrolments { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

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
    public string Status { get; set; } = "";
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
    public int TotalFinishers { get; set; }
    public string Status { get; set; } = "";
}

// ----- requests sent to the API -----

public class RegisterRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "";
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class UpdateProfileRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class EventRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public int VenueId { get; set; }
    public string EventType { get; set; } = "Run";
}

public class CategoryRequest
{
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; } = 100;
}

public class ResultRequest
{
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}

/// <summary>Optional filters for the events list on the home page.</summary>
public class EventFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int? VenueId { get; set; }
    public string? EventType { get; set; }
    public string? Q { get; set; }
}

/// <summary>An image fetched from the API (the MVC app passes it on to the browser).</summary>
public record ImagePayload(byte[] Bytes, string ContentType);