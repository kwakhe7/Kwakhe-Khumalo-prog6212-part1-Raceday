namespace RACEDAY_P3.Services;

/// <summary>The outcome of an API call. Controllers read this instead of dealing with HTTP details.</summary>
public class ApiResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    /// <summary>A friendly message that is safe to show to the user.</summary>
    public string? Error { get; init; }

    public bool Unauthorized => StatusCode == 401;
    public bool Forbidden => StatusCode == 403;
    public bool NotFound => StatusCode == 404;
}

/// <summary>Used when a call returns no body.</summary>
public sealed class NoData { }

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }

    public static ApiResult<T> Ok(int statusCode, T? data) =>
        new() { Success = true, StatusCode = statusCode, Data = data };

    public static ApiResult<T> Fail(int statusCode, string error) =>
        new() { Success = false, StatusCode = statusCode, Error = error };
}