using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace RACEDAY_P3.Services;

/// <summary>Every call the MVC app makes to the Part 2 API goes through this interface.</summary>
public interface IRaceDayApi
{
    // Auth
    Task<ApiResult<UserDto>> RegisterAsync(RegisterRequest request);
    Task<ApiResult<UserDto>> LoginAsync(LoginRequest request);
    Task<ApiResult> LogoutAsync();

    // Profile
    Task<ApiResult<UserDto>> GetMeAsync();
    Task<ApiResult<UserDto>> UpdateMeAsync(UpdateProfileRequest request);
    Task<ApiResult> UploadPictureAsync(IFormFile file);
    Task<ApiResult<ImagePayload>> GetPictureAsync(int userId);

    // Venues
    Task<ApiResult<List<VenueDto>>> GetVenuesAsync();

    // Events
    Task<ApiResult<List<EventDto>>> GetEventsAsync(EventFilter? filter = null);
    Task<ApiResult<EventDto>> GetEventAsync(int id);
    Task<ApiResult<List<EventDto>>> GetMyEventsAsync();
    Task<ApiResult<EventDto>> CreateEventAsync(EventRequest request);
    Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequest request);
    Task<ApiResult> DeleteEventAsync(int id);
    Task<ApiResult> UploadBannerAsync(int eventId, IFormFile file);
    Task<ApiResult<ImagePayload>> GetBannerAsync(int eventId);

    // Categories
    Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId);
    Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequest request);
    Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, CategoryRequest request);
    Task<ApiResult> DeleteCategoryAsync(int categoryId);

    // Enrolments
    Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId);
    Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync();
    Task<ApiResult<List<EnrolmentDto>>> GetCategoryEnrolmentsAsync(int categoryId);
    Task<ApiResult> CancelEnrolmentAsync(int enrolmentId);

    // Results
    Task<ApiResult<ResultDto>> RecordResultAsync(int enrolmentId, ResultRequest request);
    Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, ResultRequest request);
    Task<ApiResult<List<ResultDto>>> GetCategoryResultsAsync(int categoryId);
    Task<ApiResult<List<ResultDto>>> GetMyResultsAsync();
}