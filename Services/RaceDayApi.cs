using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;

namespace RACEDAY_P3.Services;

/// <summary>Typed HTTP client for the Part 2 API. Turns every failure into a friendly message.</summary>
public class RaceDayApi : IRaceDayApi
{
    private const string ServiceDown = "The RaceDay service is not responding. Please try again in a moment.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public RaceDayApi(HttpClient http) => _http = http;

    // ---------- core helpers ----------

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (content is not null)
                request.Content = content;
            else if (body is not null)
                request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);

            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                if (typeof(T) == typeof(NoData) || response.StatusCode == HttpStatusCode.NoContent)
                    return ApiResult<T>.Ok((int)response.StatusCode, default);

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return ApiResult<T>.Ok((int)response.StatusCode, data);
            }

            return ApiResult<T>.Fail((int)response.StatusCode, await FriendlyErrorAsync(response));
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Fail(0, ServiceDown);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Fail(0, ServiceDown);
        }
        catch (JsonException)
        {
            return ApiResult<T>.Fail(500, "We received an unexpected reply from the service. Please try again.");
        }
    }

    private async Task<ApiResult> ExecuteAsync(HttpMethod method, string url, object? body = null, HttpContent? content = null)
        => await SendAsync<NoData>(method, url, body, content);

    private async Task<ApiResult<ImagePayload>> GetImageAsync(string url)
    {
        try
        {
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return ApiResult<ImagePayload>.Fail((int)response.StatusCode, await FriendlyErrorAsync(response));

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return ApiResult<ImagePayload>.Ok(200, new ImagePayload(bytes, contentType));
        }
        catch (HttpRequestException)
        {
            return ApiResult<ImagePayload>.Fail(0, ServiceDown);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<ImagePayload>.Fail(0, ServiceDown);
        }
    }

    private Task<ApiResult> UploadAsync(string url, IFormFile file)
    {
        // The image is forwarded to the API, which validates it and stores it in Azure Blob Storage.
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        if (MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType))
            fileContent.Headers.ContentType = mediaType;
        content.Add(fileContent, "file", file.FileName);
        return ExecuteAsync(HttpMethod.Post, url, content: content);
    }

    /// <summary>Reads the API's error body and returns a message that is safe to show to a user.</summary>
    private static async Task<string> FriendlyErrorAsync(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        string? apiMessage = null;

        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    {
                        apiMessage = message.GetString();
                    }
                    else if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        // Validation errors from the API: { "errors": { "Email": ["..."] } }
                        var messages = new List<string>();
                        foreach (var field in errors.EnumerateObject())
                            foreach (var item in field.Value.EnumerateArray())
                                if (item.ValueKind == JsonValueKind.String)
                                    messages.Add(item.GetString()!);
                        if (messages.Count > 0)
                            apiMessage = string.Join(" ", messages.Distinct());
                    }
                }
            }
        }
        catch (Exception)
        {
            // Not JSON: fall back to the generic messages below.
        }

        return status switch
        {
            401 => apiMessage ?? "Please log in to continue.",
            403 => "You do not have permission to do that.",
            404 => apiMessage ?? "We could not find what you were looking for.",
            >= 500 and not 503 => "Something went wrong on our side. Please try again.",
            _ => apiMessage ?? "Something went wrong. Please check your details and try again."
        };
    }

    private static string Query(params (string Key, string? Value)[] pairs)
    {
        var parts = pairs
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        var query = string.Join("&", parts);
        return query.Length == 0 ? "" : "?" + query;
    }

    // ---------- auth ----------

    public Task<ApiResult<UserDto>> RegisterAsync(RegisterRequest request) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", request);

    public Task<ApiResult<UserDto>> LoginAsync(LoginRequest request) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/login", request);

    public Task<ApiResult> LogoutAsync() =>
        ExecuteAsync(HttpMethod.Post, "api/auth/logout");

    // ---------- profile ----------

    public Task<ApiResult<UserDto>> GetMeAsync() =>
        SendAsync<UserDto>(HttpMethod.Get, "api/users/me");

    public Task<ApiResult<UserDto>> UpdateMeAsync(UpdateProfileRequest request) =>
        SendAsync<UserDto>(HttpMethod.Put, "api/users/me", request);

    public Task<ApiResult> UploadPictureAsync(IFormFile file) =>
        UploadAsync("api/users/me/picture", file);

    public Task<ApiResult<ImagePayload>> GetPictureAsync(int userId) =>
        GetImageAsync($"api/users/{userId}/picture");

    // ---------- venues ----------

    public Task<ApiResult<List<VenueDto>>> GetVenuesAsync() =>
        SendAsync<List<VenueDto>>(HttpMethod.Get, "api/venues");

    // ---------- events ----------

    public Task<ApiResult<List<EventDto>>> GetEventsAsync(EventFilter? filter = null)
    {
        var query = Query(
            ("from", filter?.From?.ToString("yyyy-MM-dd")),
            ("to", filter?.To?.ToString("yyyy-MM-dd")),
            ("venueId", filter?.VenueId?.ToString()),
            ("eventType", filter?.EventType),
            ("q", filter?.Q));
        return SendAsync<List<EventDto>>(HttpMethod.Get, "api/events" + query);
    }

    public Task<ApiResult<EventDto>> GetEventAsync(int id) =>
        SendAsync<EventDto>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResult<List<EventDto>>> GetMyEventsAsync() =>
        SendAsync<List<EventDto>>(HttpMethod.Get, "api/events/mine");

    public Task<ApiResult<EventDto>> CreateEventAsync(EventRequest request) =>
        SendAsync<EventDto>(HttpMethod.Post, "api/events", request);

    public Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequest request) =>
        SendAsync<EventDto>(HttpMethod.Put, $"api/events/{id}", request);

    public Task<ApiResult> DeleteEventAsync(int id) =>
        ExecuteAsync(HttpMethod.Delete, $"api/events/{id}");

    public Task<ApiResult> UploadBannerAsync(int eventId, IFormFile file) =>
        UploadAsync($"api/events/{eventId}/banner", file);

    public Task<ApiResult<ImagePayload>> GetBannerAsync(int eventId) =>
        GetImageAsync($"api/events/{eventId}/banner");

    // ---------- categories ----------

    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId) =>
        SendAsync<List<CategoryDto>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequest request) =>
        SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", request);

    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, CategoryRequest request) =>
        SendAsync<CategoryDto>(HttpMethod.Put, $"api/categories/{categoryId}", request);

    public Task<ApiResult> DeleteCategoryAsync(int categoryId) =>
        ExecuteAsync(HttpMethod.Delete, $"api/categories/{categoryId}");

    // ---------- enrolments ----------

    public Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId) =>
        SendAsync<EnrolmentDto>(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");

    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/users/me/enrolments");

    public Task<ApiResult<List<EnrolmentDto>>> GetCategoryEnrolmentsAsync(int categoryId) =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/categories/{categoryId}/enrolments");

    public Task<ApiResult> CancelEnrolmentAsync(int enrolmentId) =>
        ExecuteAsync(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    // ---------- results ----------

    public Task<ApiResult<ResultDto>> RecordResultAsync(int enrolmentId, ResultRequest request) =>
        SendAsync<ResultDto>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/result", request);

    public Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, ResultRequest request) =>
        SendAsync<ResultDto>(HttpMethod.Put, $"api/results/{resultId}", request);

    public Task<ApiResult<List<ResultDto>>> GetCategoryResultsAsync(int categoryId) =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/categories/{categoryId}/results");

    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync() =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, "api/users/me/results");
}