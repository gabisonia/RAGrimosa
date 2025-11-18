using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using RAGrimosa.Models;
using RAGrimosa.Options;

namespace RAGrimosa.Services;

/// <summary>
/// Coordinates the end-to-end RAG flow: ingestion, conversational loop, and chat responses.
/// </summary>
internal sealed class RagOrchestrator(
    TextIngestionService ingestionService,
    VectorStoreCollection<string, DocumentChunk> collection,
    IChatClient chatClient,
    IOptions<RagOptions> ragOptions,
    ILogger<RagOrchestrator> logger)
{
    /// <summary>
    /// Runs ingestion once and then starts the interactive question-answer loop until cancellation.
    /// </summary>
    /// <param name="cancellationToken">Token used to terminate ingestion or the conversation flow.</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        WriteLine(ConsoleColor.Cyan, "Starting ingestion...\n");

        try
        {
            await ingestionService.IngestAsync(cancellationToken);
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

            var searchResults = await SearchContextAsync(question, ragSettings.SearchResultCount, cancellationToken);

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
                var response = await chatClient.GetResponseAsync(chatMessages, cancellationToken: cancellationToken);

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

    /// <summary>
    /// Executes a vector search for the supplied query and gathers all results into a list.
    /// </summary>
    /// <param name="query">User question that needs additional context.</param>
    /// <param name="top">Maximum number of search matches to return.</param>
    /// <param name="cancellationToken">Used to cancel the search enumeration.</param>
    /// <returns>Collection of vector matches ordered by similarity.</returns>
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

    /// <summary>
    /// Formats retrieved chunks into a textual section that will be prepended to the user message.
    /// </summary>
    /// <param name="results">Vector search matches.</param>
    /// <returns>Multi-line block containing numbered snippets and chunk metadata.</returns>
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

    /// <summary>
    /// Prints a concise list of retrieved chunks so the console output shows which sources were used.
    /// </summary>
    /// <param name="results">Vector search matches that grounded the answer.</param>
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

    /// <summary>
    /// Writes a colored line to the console, restoring the original color afterwards.
    /// </summary>
    /// <param name="color">Foreground color to apply.</param>
    /// <param name="message">Message to print.</param>
    private static void WriteLine(ConsoleColor color, string message)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = original;
    }

    /// <summary>
    /// Writes a colored message without appending a newline.
    /// </summary>
    /// <param name="color">Foreground color to apply.</param>
    /// <param name="message">Message to print.</param>
    private static void Write(ConsoleColor color, string message)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(message);
        Console.ForegroundColor = original;
    }
}