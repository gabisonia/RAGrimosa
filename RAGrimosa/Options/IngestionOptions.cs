using System.ComponentModel.DataAnnotations;

namespace RAGrimosa.Options;

internal sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";
    [Required] public string InputFilePath { get; init; } = string.Empty;
    [Range(128, 8192)] public int ChunkSize { get; init; } = 1024;
    [Range(0, 4096)] public int ChunkOverlap { get; init; } = 128;

    public bool RecreateCollection { get; init; }
        = false;
}