using RACEDAY_P3.Services;

namespace RACEDAY_P3.Models;

/// <summary>One line of the race history: the API's result plus the event date (taken from the enrolment).</summary>
public class ResultRowViewModel
{
    public ResultDto Result { get; set; } = new();
    public DateOnly? EventDate { get; set; }
}

public class MyResultsViewModel
{
    public List<ResultRowViewModel> Rows { get; set; } = new();
    public int RacesFinished { get; set; }
    public int? BestPosition { get; set; }
}

/// <summary>Small helpers for showing race results the same way on every page.</summary>
public static class RaceFormat
{
    /// <summary>1 becomes "1st", 2 "2nd", 11 "11th", 47 "47th".</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13) return $"{number}th";

        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    public static string Time(TimeSpan? time) =>
        time.HasValue ? time.Value.ToString(@"hh\:mm\:ss") : "-";
}