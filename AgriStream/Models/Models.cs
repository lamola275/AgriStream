// All the little data shapes the MVC app uses, in one file.
namespace AgriStream.Models;

// What the dashboard page needs to display
public class DashboardViewModel
{
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
    public List<TelemetryReading> Readings { get; set; } = new();
}

// One telemetry row (matches the JSON the GET function returns)
public class TelemetryReading
{
    public string DeviceId { get; set; } = "";
    public string Sensor { get; set; } = "";
    public double Value { get; set; }
    public string Time { get; set; } = "";
}
