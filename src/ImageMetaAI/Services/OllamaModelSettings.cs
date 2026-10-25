namespace ImageMetaAI.Services;

public class OllamaModelSettings
{
    public string VisionModel { get; init; } = "qwen2.5vl:7b";

    public string MetadataModel { get; init; } = "qwen3:14b";
}
