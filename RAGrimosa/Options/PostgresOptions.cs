using System.ComponentModel.DataAnnotations;

namespace RAGrimosa.Options;

internal sealed class PostgresOptions
{
    public const string SectionName = "Postgres";
    [Required] public string ConnectionString { get; init; } = string.Empty;
    [Required] public string CollectionName { get; init; } = string.Empty;
}