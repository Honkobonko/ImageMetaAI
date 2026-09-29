namespace ImageMetaAI.Models;

public class ImageMetadata
{
    public string Title { get; set; } = string.Empty;

    public IReadOnlyList<string> Keywords { get; set; } = [];

    public string Category { get; set; } = string.Empty;

    public string? SecondaryCategory { get; set; }
}
