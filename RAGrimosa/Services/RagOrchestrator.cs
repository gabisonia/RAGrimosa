using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using RAGrimosa.Models;
using RAGrimosa.Options;

namespace RAGrimosa.Services;

internal sealed class RagOrchestrator(
    TextIngestionService ingestionService,
    VectorStoreCollection<string, DocumentChunk> collection,
    IChatClient chatClient,
    IOptions<RagOptions> ragOptions,
    ILogger<RagOrchestrator> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        WriteLine(ConsoleColor.Cyan, "Starting ingestion...\n");

        try
        {
            await ingestionService.IngestAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data ingestion failed");
            WriteLine(ConsoleColor.Red, "Ingestion failed. See logs for details.");
            return;
        }

        WriteLine(ConsoleColor.Green, "Ingestion complete. Ask a question (empty line to exit).");

        var ragSettings = ragOptions.Value;

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine();
            Write(ConsoleColor.Green, "user > ");

            var question = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(question))
            {
                break;
            }

            var searchResults = await SearchContextAsync(question, ragSettings.SearchResultCount, cancellationToken)
                .ConfigureAwait(false);
            if (searchResults.Count == 0)
            {
                WriteLine(ConsoleColor.Yellow, "No relevant context retrieved. Try another question.");
                continue;
            }

            var chatMessages = new[]
            {
                new ChatMessage(ChatRole.System, ragSettings.SystemPrompt),
                new ChatMessage(ChatRole.User,
                    $"{BuildContextSection(searchResults)}Question:\n{question}"),
            };

            try
            {
                var response = await chatClient.GetResponseAsync(chatMessages, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                Console.WriteLine();
                WriteLine(ConsoleColor.Cyan, $"assistant > {response.Text}".Trim());

                PrintCitations(searchResults);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get response from chat model");
                WriteLine(ConsoleColor.Red, "Chat request failed. Check configuration and try again.");
            }
        }
    }

    private async Task<List<VectorSearchResult<DocumentChunk>>> SearchContextAsync(
        string query,
        int top,
        CancellationToken cancellationToken)
    {
        var results = new List<VectorSearchResult<DocumentChunk>>();
        await foreach (var result in collection.SearchAsync(query, top, cancellationToken: cancellationToken))
        {
            results.Add(result);
        }

        return results;
    }

    private static string BuildContextSection(IReadOnlyList<VectorSearchResult<DocumentChunk>> results)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            "Use the numbered snippets below to ground your answer, but keep the reply free of citation markers.");
        builder.AppendLine();

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var record = result.Record;
            var scoreText = result.Score is null ? string.Empty : $" (score: {result.Score:0.###})";
            builder.AppendLine($"[{i + 1}] {record.Source} chunk {record.ChunkIndex}{scoreText}");
            builder.AppendLine(record.Content);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void PrintCitations(IReadOnlyList<VectorSearchResult<DocumentChunk>> results)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.White, "Context chunks:");

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var record = result.Record;
            var scoreText = result.Score is null ? string.Empty : $" score={result.Score:0.###}";
            WriteLine(ConsoleColor.White, $"[{i + 1}] {record.Source} chunk {record.ChunkIndex}{scoreText}");
        }
    }

    private static void WriteLine(ConsoleColor color, string message)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = original;
    }

    private static void Write(ConsoleColor color, string message)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(message);
        Console.ForegroundColor = original;
    }
}