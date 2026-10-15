namespace RaceDay.Models;

public enum UserRole { Organiser = 1, Participant = 2 }

public enum EventType { Run = 1, Walk = 2, Cycle = 3 }

public enum EnrolmentStatus { Confirmed = 1, Cancelled = 2 }

public enum ResultStatus { Finished = 1, DNF = 2, DQ = 3 }

/// <summary>Role names used in [Authorize(Roles = ...)] and in the session claims.</summary>
public static class AppRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}