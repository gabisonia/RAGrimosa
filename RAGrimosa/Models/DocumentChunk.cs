using Microsoft.Extensions.VectorData;

namespace RAGrimosa.Models;

internal sealed class DocumentChunk
{
    [VectorStoreKey] public required string Id { get; init; }
    [VectorStoreData] public required string Content { get; init; }
    [VectorStoreData] public required string Source { get; init; }
    [VectorStoreData] public int ChunkIndex { get; init; }

    [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public string Embedding => Content;
}