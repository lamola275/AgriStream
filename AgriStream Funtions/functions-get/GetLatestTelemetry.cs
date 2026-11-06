using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class GetLatestTelemetry
{
    private readonly ILogger<GetLatestTelemetry> _logger;

    public GetLatestTelemetry(ILogger<GetLatestTelemetry> logger)
    {
        _logger = logger;
    }

    [Function("GetLatestTelemetry")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "telemetry")]
        HttpRequestData req)
    {
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        try
        {
            // CHECK 1: does the setting exist at all?
            var connStr = Environment.GetEnvironmentVariable("TableConnectionString");
            if (string.IsNullOrEmpty(connStr))
            {
                await response.WriteStringAsync(
                    "DIAGNOSIS: TableConnectionString is MISSING on this Function App. " +
                    "Add it under Settings -> Environment variables, Save, then Restart.");
                return response;
            }

            // CHECK 2: does it look like a connection string?
            if (!connStr.Contains("AccountName="))
            {
                await response.WriteStringAsync(
                    "DIAGNOSIS: TableConnectionString exists but is malformed. Starts with: "
                    + connStr.Substring(0, Math.Min(40, connStr.Length)));
                return response;
            }

            var table = new TableClient(connStr, "telemetry");

            var readings = new List<TelemetryReading>();
            await foreach (var e in table.QueryAsync<TableEntity>())
            {
                readings.Add(new TelemetryReading
                {
                    DeviceId = e.PartitionKey,
                    Sensor = e.GetString("Sensor") ?? "",
                    Value = e.TryGetValue("Value", out object? v) ? Convert.ToDouble(v) : 0,
                    Time = e.Timestamp?.UtcDateTime.ToString("u") ?? ""
                });
            }

            var latest = readings.OrderByDescending(r => r.Time).Take(20).ToList();
            await response.WriteAsJsonAsync(latest);
            return response;
        }
        catch (Exception ex)
        {
            // Show the REAL error instead of a blank 500
            await response.WriteStringAsync("DIAGNOSIS: " + ex.GetType().Name + " - " + ex.Message);
            return response;
        }
    }
}

public class TelemetryReading
{
    public string DeviceId { get; set; } = "";
    public string Sensor { get; set; } = "";
    public double Value { get; set; }
    public string Time { get; set; } = "";
}