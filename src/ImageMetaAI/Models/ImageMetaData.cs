namespace ImageMetaAI.Models;

public class ImageMetadata
{
    public string Title { get; init; } = string.Empty;

    public IReadOnlyList<string> Keywords { get; init; } = [];
}