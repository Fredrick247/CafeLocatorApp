using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CafeLocatorApp.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CafeLocatorApp.Controllers;
[Route("api/[controller]")] 
[ApiController]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMemoryCache _cache;
    public HomeController(ILogger<HomeController> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("GetCafes")] // ✅ Ensures API endpoint is properly mapped
    public async Task<IActionResult> GetCafes(double latitude, double longitude, int radius = 5000, int page = 1, int pageSize = 10)
    {
        string cacheKey = $"cafes-{latitude}-{longitude}-{radius}";

        if (!_cache.TryGetValue(cacheKey, out List<Cafe> cafes))
        {
            cafes = await CafeService.FindNearbyCafeAsync(latitude, longitude, radius);
            _cache.Set(cacheKey, cafes, TimeSpan.FromMinutes(10)); // Cache for 10 mins
        }
        var paginatedCafes = cafes.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(cafes);
    }
}