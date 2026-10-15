namespace RaceDay.Models;

public class AppUser
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Venue
{
    public int VenueId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public int Capacity { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public int VenueId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public EventType EventType { get; set; }
    public DateTime CreatedAt { get; set; }

    public AppUser Organiser { get; set; } = null!;
    public Venue Venue { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; } = 100;

    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Confirmed;

    public AppUser Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public ResultStatus Status { get; set; } = ResultStatus.Finished;

    public Enrolment Enrolment { get; set; } = null!;
}