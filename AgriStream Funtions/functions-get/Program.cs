// Entry point for the Function App (isolated worker model).
// You don't need to touch this file - it just starts the worker.
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
