using System.ComponentModel.DataAnnotations;

namespace RAGrimosa.Options;

internal sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    [Required] public string ApiKey { get; init; } = string.Empty;
    [Required] public string ChatModel { get; init; } = string.Empty;
    [Required] public string EmbeddingModel { get; init; } = string.Empty;
}