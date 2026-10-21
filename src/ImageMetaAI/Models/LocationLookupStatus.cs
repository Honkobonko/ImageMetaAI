namespace ImageMetaAI.Models;

public enum LocationLookupStatus
{
    NotAttempted,
    Disabled,
    NoGpsData,
    Found,
    NotFound,
    Failed
}
