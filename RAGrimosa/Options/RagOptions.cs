using System.ComponentModel.DataAnnotations;

namespace RAGrimosa.Options;

internal sealed class RagOptions
{
    public const string SectionName = "Rag";
    [Range(1, 50)] public int SearchResultCount { get; init; } = 5;
    [Required] public string SystemPrompt { get; init; } = string.Empty;
}