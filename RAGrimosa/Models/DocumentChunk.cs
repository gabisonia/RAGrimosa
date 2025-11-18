using Microsoft.Extensions.VectorData;

namespace RAGrimosa.Models;

/// <summary>
/// Represents a single ingested chunk along with metadata needed for retrieval and grounding.
/// </summary>
internal sealed class DocumentChunk
{
    /// <summary>
    /// Stable primary key used by the vector store so re-ingestion overwrites the same record.
    /// </summary>
    [VectorStoreKey]
    public required string Id { get; init; }

    /// <summary>
    /// Actual snippet text that is stored as plain metadata and surfaced in responses.
    /// </summary>
    [VectorStoreData]
    public required string Content { get; init; }

    /// <summary>
    /// Original document name, stored alongside the chunk for citation purposes.
    /// </summary>
    [VectorStoreData]
    public required string Source { get; init; }

    /// <summary>
    /// Sequential position of the chunk within its source file.
    /// </summary>
    [VectorStoreData]
    public int ChunkIndex { get; init; }

    /// <summary>
    /// Vector column definition; returning Content signals the vector store to embed the snippet text.
    /// </summary>
    [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public string Embedding => Content;
}