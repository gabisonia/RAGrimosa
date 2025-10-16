using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;
using RAGrimosa.Models;
using RAGrimosa.Options;
using RAGrimosa.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

builder.Services
    .AddLogging(logging => logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Information));

builder.Services.AddOptions<OpenAiOptions>()
    .Bind(builder.Configuration.GetSection(OpenAiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<PostgresOptions>()
    .Bind(builder.Configuration.GetSection(PostgresOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<IngestionOptions>()
    .Bind(builder.Configuration.GetSection(IngestionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RagOptions>()
    .Bind(builder.Configuration.GetSection(RagOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddEmbeddingGenerator(sp =>
{
    var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
    return new EmbeddingClient(options.EmbeddingModel, options.ApiKey).AsIEmbeddingGenerator();
});

builder.Services.AddChatClient(sp =>
{
    var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
    return new ChatClient(options.ChatModel, options.ApiKey).AsIChatClient();
});

builder.Services.AddSingleton<TextIngestionService>();
builder.Services.AddSingleton<RagOrchestrator>();

var postgresConfiguration = builder.Configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>();
if (postgresConfiguration is null)
{
    throw new InvalidOperationException("Postgres configuration is missing.");
}

if (string.IsNullOrWhiteSpace(postgresConfiguration.CollectionName))
{
    throw new InvalidOperationException("Postgres collection name must be configured (Postgres:CollectionName).");
}

if (string.IsNullOrWhiteSpace(postgresConfiguration.ConnectionString))
{
    throw new InvalidOperationException("Postgres connection string must be configured (Postgres:ConnectionString).");
}

builder.Services.AddPostgresCollection<string, DocumentChunk>(
    postgresConfiguration.CollectionName,
    postgresConfiguration.ConnectionString);

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
