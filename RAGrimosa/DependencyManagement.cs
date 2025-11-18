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

namespace RAGrimosa;

internal static class DependencyManagement
{
    public static void Configure(HostApplicationBuilder builder)
    {
        ConfigureConfiguration(builder);
        ConfigureLogging(builder);
        ConfigureOptions(builder);
        ConfigureOpenAiClients(builder);
        ConfigureVectorData(builder);
        RegisterApplicationServices(builder);
    }

    private static void ConfigureConfiguration(HostApplicationBuilder builder)
    {
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true);
    }

    private static void ConfigureLogging(HostApplicationBuilder builder)
    {
        builder.Services
            .AddLogging(logging => logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Information));
    }

    private static void ConfigureOptions(HostApplicationBuilder builder)
    {
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
    }

    private static void ConfigureOpenAiClients(HostApplicationBuilder builder)
    {
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
    }

    private static void ConfigureVectorData(HostApplicationBuilder builder)
    {
        var postgresConfiguration =
            builder.Configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>();
        if (postgresConfiguration is null)
        {
            throw new InvalidOperationException("Postgres configuration is missing.");
        }

        if (string.IsNullOrWhiteSpace(postgresConfiguration.CollectionName))
        {
            throw new InvalidOperationException(
                "Postgres collection name must be configured (Postgres:CollectionName).");
        }

        if (string.IsNullOrWhiteSpace(postgresConfiguration.ConnectionString))
        {
            throw new InvalidOperationException(
                "Postgres connection string must be configured (Postgres:ConnectionString).");
        }

        builder.Services.AddPostgresCollection<string, DocumentChunk>(
            postgresConfiguration.CollectionName,
            postgresConfiguration.ConnectionString);
    }

    private static void RegisterApplicationServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<TextIngestionService>();
        builder.Services.AddSingleton<RagOrchestrator>();
    }
}