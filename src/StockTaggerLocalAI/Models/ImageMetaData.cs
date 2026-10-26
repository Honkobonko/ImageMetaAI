using System.Text.Json.Serialization;

namespace StockTaggerLocalAI.Models;

public class ImageMetadata
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("keywords")]
    public IReadOnlyList<string> Keywords { get; set; } = [];

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("secondaryCategory")]
    public string? SecondaryCategory { get; set; }
}