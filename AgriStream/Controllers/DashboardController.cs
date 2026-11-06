using System.Text;
using System.Text.Json;
using AgriStream.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Controllers;

public class DashboardController : Controller
{
    private readonly IConfiguration _config;

    public DashboardController(IConfiguration config)
    {
        _config = config;
    }

    // GET /Dashboard - the main page
    // GET /Dashboard - the main page
    public async Task<IActionResult> Index()
    {
        // Only logged-in users allowed
        var username = HttpContext.Session.GetString("username");
        if (username == null) return RedirectToAction("Login", "Account");

        var readings = new List<TelemetryReading>();
        var telemetryUrl = _config["TelemetryFunctionUrl"];

        if (string.IsNullOrEmpty(telemetryUrl) || !telemetryUrl.StartsWith("http"))
        {
            ViewBag.Error = "Telemetry function not deployed yet - skipping live data.";
        }
        else
        {
            try
            {
                using var http = new HttpClient();
                var json = await http.GetStringAsync(telemetryUrl);
                readings = JsonSerializer.Deserialize<List<TelemetryReading>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch (Exception ex)                                          // <-- CHANGE HERE
            {                                                             // <-- (was just "catch")
                ViewBag.Error = "Telemetry call failed: " + ex.Message;   // <-- new message
            }
        }

        return View(new DashboardViewModel
        {
            Username = username,
            Role = HttpContext.Session.GetString("role") ?? "",
            Readings = readings
        });
    }

    // POST /Dashboard/UploadImage - drone image -> Blob Storage
    // (the Blob-triggered function picks it up from there automatically)
    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            var container = new BlobContainerClient(
                _config["BlobConnectionString"], "drone-images");
            await container.CreateIfNotExistsAsync();
            await container.UploadBlobAsync(file.FileName, file.OpenReadStream());
            TempData["Message"] = $"Uploaded '{file.FileName}' - the Blob function is auditing it.";
        }
        return RedirectToAction("Index");
    }

    // POST /Dashboard/Irrigate - button -> call the HTTP POST function
    [HttpPost]
    public async Task<IActionResult> Irrigate(string deviceId)
    {
        using var http = new HttpClient();
        var body = new StringContent(
            JsonSerializer.Serialize(new { deviceId }),
            Encoding.UTF8, "application/json");
        await http.PostAsync(_config["IrrigationFunctionUrl"], body);

        TempData["Message"] = $"Irrigation command queued for {deviceId}. " +
            "Check DeviceRegistry in SQL - the Queue function will update it.";
        return RedirectToAction("Index");
    }
}
