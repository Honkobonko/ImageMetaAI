using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public class MetadataGenerator : IMetadataGenerator
{
    private readonly IOllamaClient _ollamaClient;
    private readonly OllamaModelSettings _modelSettings;

    public MetadataGenerator(
        IOllamaClient ollamaClient,
        OllamaModelSettings modelSettings)
    {
        _ollamaClient = ollamaClient;
        _modelSettings = modelSettings;
    }

    public async Task<ImageMetadata> GenerateAsync(
        string visionDescription,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(visionDescription))
        {
            throw new ArgumentException(
                "Vision description must not be empty.",
                nameof(visionDescription));
        }

        const string prompt = """
            Create accurate, searchable stock photography metadata
            in English based only on the image description.

            TITLE
            - Write a clear, specific, natural stock photo title.
            - Use at least 5 words.
            - Describe the main subject and relevant visible context.
            - Avoid keyword lists, repetition, and unnecessary filler.
            - Do not exceed 2048 characters.

            KEYWORDS
            - Generate 20 to 30 distinct, relevant keywords.
            - Use individual words or concise phrases where appropriate.
            - Put the most important and specific keywords first.
            - Prioritize the main subject, visible objects, actions,
              setting, composition, and clearly supported concepts.
            - Avoid synonyms that add little meaning, repeated ideas,
              redundant variations, and generic filler.
            - Every keyword must add useful search value.
            - Do not include unsupported assumptions.

            CATEGORY ANS SECONDARYCATEGORY
            Choose the best category and, if useful, a different
            secondary category from this exact list:
            - Abstract
            - Animals/Wildlife
            - Arts
            - Backgrounds/Textures
            - Beauty/Fashion
            - Buildings/Landmarks
            - Business/Finance
            - Celebrities
            - Education
            - Food and Drink
            - Healthcare/Medical
            - Holidays
            - Industrial
            - Interiors
            - Miscellaneous
            - Nature
            - Objects
            - Parks/Outdoor
            - People
            - Religion
            - Science
            - Signs/Symbols
            - Sports/Recreation
            - Technology
            - Transportation
            - Vintage

            CATEGORY RULES
            - The primary category is required.
            - Choose the category that best represents the main subject.
            - Use the secondary category only when it adds a meaningful,
              distinct classification.
            - Otherwise, set secondaryCategory to null.
            - Never choose the same category twice.
            - Select only categories from the supplied list.

            ACCURACY
            - Use only information supported by the image description.
            - Do not invent names, locations, brands, professions,
              events, or other details.
            - If a detail is uncertain, leave it out.

            Return only the metadata matching the required JSON schema.

            Image description:
            """;

        var fullPrompt = $"{prompt}\n{visionDescription}";

        return await _ollamaClient.GenerateMetadataAsync(
            _modelSettings.MetadataModel,
            fullPrompt,
            cancellationToken);
    }
}
