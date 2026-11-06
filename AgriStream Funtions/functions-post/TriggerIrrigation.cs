// ============================================================
//  FUNCTION 3 of 4 - HTTP POST TRIGGER (Task d)
//  Called when a sensor flags a problem or a user clicks
//  "Trigger Irrigation" in the MVC app.
//  Job: drop an "Irrigation Command" message onto the
//       Azure Storage Queue (fast, then the user is free -
//       this is what "decoupling" means).
// ============================================================

using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class TriggerIrrigation
{
    private readonly ILogger<TriggerIrrigation> _logger;

    public TriggerIrrigation(ILogger<TriggerIrrigation> logger)
    {
        _logger = logger;
    }

    // Route "irrigate" => the URL is /api/irrigate
    [Function("TriggerIrrigation")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "irrigate")]
        HttpRequestData req)
    {
        var command = await JsonSerializer.DeserializeAsync<IrrigationCommand>(req.Body)
                      ?? new IrrigationCommand { DeviceId = "pump-01" };

        // Drop the command onto the queue and return immediately
        var queue = new QueueClient(
            Environment.GetEnvironmentVariable("TableConnectionString"), "irrigation-commands");
        await queue.CreateIfNotExistsAsync();
        await queue.SendMessageAsync(JsonSerializer.Serialize(command));

        _logger.LogInformation("Irrigation command queued for {Device}", command.DeviceId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { message = $"Irrigation queued for {command.DeviceId}" });
        return response;
    }
}

public class IrrigationCommand
{
    public string DeviceId { get; set; } = "";
}
