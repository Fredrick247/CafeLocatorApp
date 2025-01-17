using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CafeLocatorApp.Models;

namespace CafeLocatorApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet] // Ensure it's GET
    public async Task<IActionResult> GetCafes(double latitude, double longitude)
    {
        try
        {
            List<Cafe> cafes = await CafeService.FindNearbyCafeAsync(latitude, longitude);
            return Json(cafes);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}