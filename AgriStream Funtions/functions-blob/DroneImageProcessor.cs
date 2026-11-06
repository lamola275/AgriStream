// ============================================================
//  FUNCTION 1 of 4 - BLOB TRIGGER (Task d)
//  Fires automatically when a drone image lands in Blob Storage.
//  Job: log the upload + write an audit entry into Table Storage.
// ============================================================

using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class DroneImageProcessor
{
    private readonly ILogger<DroneImageProcessor> _logger;

    public DroneImageProcessor(ILogger<DroneImageProcessor> logger)
    {
        _logger = logger;
    }

    // "drone-images/{name}" = watch the drone-images blob container.
    // Connection = the app setting holding the storage connection string.
    [Function("DroneImageProcessor")]
    public async Task Run(
        [BlobTrigger("drone-images/{name}", Connection = "BlobConnectionString")]
        Stream image, string name)
    {
        _logger.LogInformation("Drone image received: {Name} ({Size} bytes)", name, image.Length);

        // Write the audit entry into the "audit" table (Table Storage)
        var table = new TableClient(
            Environment.GetEnvironmentVariable("TableConnectionString"), "audit");
        await table.CreateIfNotExistsAsync();

        var entry = new TableEntity("drone-upload", Guid.NewGuid().ToString())
        {
            { "FileName", name },
            { "SizeBytes", image.Length },
            { "Status", "Uploaded" }
        };
        await table.AddEntityAsync(entry);

        _logger.LogInformation("Audit entry written for {Name}", name);
    }
}
