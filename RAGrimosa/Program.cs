using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RAGrimosa;
using RAGrimosa.Services;

var builder = Host.CreateApplicationBuilder(args);

DependencyManagement.Configure(builder);

using var host = builder.Build();

using var cancellation = new CancellationTokenSource();
var cancellationTokenSource = cancellation;
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

var orchestrator = host.Services.GetRequiredService<RagOrchestrator>();
await orchestrator.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);