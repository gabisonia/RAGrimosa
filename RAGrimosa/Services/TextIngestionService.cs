using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using RAGrimosa.Models;
using RAGrimosa.Options;

namespace RAGrimosa.Services;

internal sealed class TextIngestionService(
    VectorStoreCollection<string, DocumentChunk> collection,
    IOptions<IngestionOptions> options,
    ILogger<TextIngestionService> logger)
{
    public async Task<int> IngestAsync(CancellationToken cancellationToken)
    {
        var ingestionOptions = options.Value;
        if (!File.Exists(ingestionOptions.InputFilePath))
        {
            throw new FileNotFoundException("Input file for ingestion was not found.", ingestionOptions.InputFilePath);
        }

        await EnsureCollectionAsync(ingestionOptions.RecreateCollection, cancellationToken).ConfigureAwait(false);

        var fileContent = await File.ReadAllTextAsync(ingestionOptions.InputFilePath, cancellationToken)
            .ConfigureAwait(false);
        var chunks = SplitIntoChunks(fileContent, ingestionOptions.ChunkSize, ingestionOptions.ChunkOverlap);

        if (chunks.Count == 0)
        {
            logger.LogWarning("No content extracted from {FilePath}; skipping ingestion",
                ingestionOptions.InputFilePath);
            return 0;
        }

        var sourceName = Path.GetFileName(ingestionOptions.InputFilePath);
        var records = new List<DocumentChunk>(chunks.Count);
        for (var index = 0; index < chunks.Count; index++)
        {
            records.Add(new DocumentChunk
            {
                Id = CreateStableChunkId(sourceName, index),
                Content = chunks[index],
                Source = sourceName,
                ChunkIndex = index,
            });
        }

        await collection.UpsertAsync(records, cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Ingested {ChunkCount} chunks from {FilePath}", records.Count,
            ingestionOptions.InputFilePath);
        return records.Count;
    }

    private async Task EnsureCollectionAsync(bool recreateRequested, CancellationToken cancellationToken)
    {
        if (recreateRequested)
        {
            await collection.EnsureCollectionDeletedAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "RecreateCollection is enabled. Existing chunks will be overwritten when keys match. Use a new collection name to start fresh.");
        }

        await collection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> SplitIntoChunks(string content, int chunkSize, int overlap)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<string>();
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero.");
        }

        overlap = Math.Clamp(overlap, 0, chunkSize - 1);
        var normalized = content.ReplaceLineEndings("\n");
        var step = chunkSize - overlap;

        var chunks = new List<string>((normalized.Length + step - 1) / step);
        for (var start = 0; start < normalized.Length; start += step)
        {
            var end = Math.Min(start + chunkSize, normalized.Length);
            var chunk = normalized[start..end].Trim();
            if (chunk.Length == 0)
            {
                continue;
            }

            chunks.Add(chunk);

            if (end == normalized.Length)
            {
                break;
            }
        }

        return chunks;
    }

    private static string CreateStableChunkId(string sourceName, int index)
    {
        var sanitizedSource = Path.GetFileNameWithoutExtension(sourceName)?.Replace(' ', '_').ToLowerInvariant();
        return $"{sanitizedSource}-{index:D4}";
    }
}