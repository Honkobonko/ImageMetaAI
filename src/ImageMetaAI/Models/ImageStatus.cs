namespace ImageMetaAI.Models;

public enum ImageStatus
{
    Open,
    Analyzing,
    AnalysisCompleted,
    GeneratingMetadata,
    MetadataCompleted,
    Error,
    Cancelled,
    Skipped
}