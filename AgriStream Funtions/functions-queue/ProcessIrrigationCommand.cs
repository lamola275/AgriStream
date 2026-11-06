using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class ProcessIrrigationCommand
{
    private readonly ILogger<ProcessIrrigationCommand> _logger;

    public ProcessIrrigationCommand(ILogger<ProcessIrrigationCommand> logger)
    {
        _logger = logger;
    }

    // Listens to the "irrigation-commands" Storage Queue
    // (in the agristreamtables account).
    [Function("ProcessIrrigationCommand")]
    public async Task Run(
        [QueueTrigger("irrigation-commands", Connection = "TableConnectionString")]
        string message)
    {
        var command = JsonSerializer.Deserialize<IrrigationCommand>(message)
                      ?? new IrrigationCommand { DeviceId = "unknown" };

        _logger.LogInformation("Processing irrigation for {Device}", command.DeviceId);

        // Update the master registry table in Azure SQL
        await using var conn = new SqlConnection(
            Environment.GetEnvironmentVariable("SqlConnectionString"));
        await conn.OpenAsync();

        var update = new SqlCommand(
            "UPDATE DeviceRegistry SET Status = 'Irrigating', " +
            "LastUpdated = GETUTCDATE() WHERE DeviceId = @id", conn);
        update.Parameters.AddWithValue("@id", command.DeviceId);
        int rows = await update.ExecuteNonQueryAsync();

        _logger.LogInformation("Device {Device} set to Irrigating ({Rows} row updated)",
            command.DeviceId, rows);
    }
}

public class IrrigationCommand
{
    public string DeviceId { get; set; } = "";
}