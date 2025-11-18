using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using RAGrimosa.Models;
using RAGrimosa.Options;

namespace RAGrimosa.Services;

/// <summary>
/// Handles reading source documents, chunking them, and persisting embeddings into the vector store.
/// </summary>
internal sealed class TextIngestionService(
    VectorStoreCollection<string, DocumentChunk> collection,
    IOptions<IngestionOptions> options,
    ILogger<TextIngestionService> logger)
{
    /// <summary>
    /// Ingests the configured text file into the vector store, returning the number of chunks written.
    /// </summary>
    /// <param name="cancellationToken">Stops file IO or vector store operations when triggered.</param>
    public async Task<int> IngestAsync(CancellationToken cancellationToken)
    {
        var ingestionOptions = options.Value;
        if (!File.Exists(ingestionOptions.InputFilePath))
        {
            throw new FileNotFoundException("Input file for ingestion was not found.", ingestionOptions.InputFilePath);
        }

        await EnsureCollectionAsync(ingestionOptions.RecreateCollection, cancellationToken);

        var fileContent = await File.ReadAllTextAsync(ingestionOptions.InputFilePath, cancellationToken);
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

        await collection.UpsertAsync(records, cancellationToken: cancellationToken);

        logger.LogInformation("Ingested {ChunkCount} chunks from {FilePath}", records.Count,
            ingestionOptions.InputFilePath);
        return records.Count;
    }

    /// <summary>
    /// Ensures the backing collection exists, optionally deleting the previous contents first.
    /// </summary>
    /// <param name="recreateRequested">Whether an existing collection should be removed.</param>
    /// <param name="cancellationToken">Cancels collection operations.</param>
    private async Task EnsureCollectionAsync(bool recreateRequested, CancellationToken cancellationToken)
    {
        if (recreateRequested)
        {
            await collection.EnsureCollectionDeletedAsync(cancellationToken);
            logger.LogInformation(
                "RecreateCollection is enabled. Existing chunks will be overwritten when keys match. Use a new collection name to start fresh.");
        }

        await collection.EnsureCollectionExistsAsync(cancellationToken);
    }

    /// <summary>
    /// Splits input text into overlapping chunks that align with the configured chunk size.
    /// </summary>
    /// <param name="content">Source text to process.</param>
    /// <param name="chunkSize">Maximum number of characters per chunk.</param>
    /// <param name="overlap">Number of characters that consecutive chunks should share.</param>
    /// <returns>Ordered list of trimmed, non-empty chunks.</returns>
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

    /// <summary>
    /// Creates a deterministic identifier for each chunk so re-ingestion overwrites existing rows.
    /// </summary>
    /// <param name="sourceName">Original file name.</param>
    /// <param name="index">Chunk index within the file.</param>
    /// <returns>Stable string identifier composed of the normalized file name and a zero-padded index.</returns>
    private static string CreateStableChunkId(string sourceName, int index)
    {
        var sanitizedSource = Path.GetFileNameWithoutExtension(sourceName)?.Replace(' ', '_').ToLowerInvariant();
        return $"{sanitizedSource}-{index:D4}";
    }
}