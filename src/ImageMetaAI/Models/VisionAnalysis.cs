namespace ImageMetaAI.Models;

public class VisionAnalysis
{
    public string Description { get; init; } = string.Empty;

    public List<string> Objects { get; init; } = [];

    public List<string> Activities { get; init; } = [];

    public List<string> Settings { get; init; } = [];
}