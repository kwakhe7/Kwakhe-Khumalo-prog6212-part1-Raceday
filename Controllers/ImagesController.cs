using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RACEDAY_P3.Services;

namespace RACEDAY_P3.Controllers;

/// <summary>
/// Serves images to the browser. The pictures live in Azure Blob Storage, but only the API may talk to Azure:
/// this controller fetches each image from the API and passes it on.
/// </summary>
public class ImagesController : AppController
{
    private readonly IRaceDayApi _api;

    public ImagesController(IRaceDayApi api) => _api = api;

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Banner(int id)
    {
        var result = await _api.GetBannerAsync(id);
        if (!result.Success || result.Data is null) return NotFound();
        return File(result.Data.Bytes, result.Data.ContentType);
    }

    [HttpGet]
    [Authorize]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Picture(int id)
    {
        var result = await _api.GetPictureAsync(id);
        if (!result.Success || result.Data is null) return NotFound();
        return File(result.Data.Bytes, result.Data.ContentType);
    }
}