using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockTaggerLocalAI.Models;

namespace StockTaggerLocalAI.Services;

public class OllamaClient : IOllamaClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OllamaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                "/api/tags",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<OllamaModel>> GetModelsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<OllamaModelsResponse>(
            "/api/tags",
            cancellationToken);

        return response?.Models ?? [];
    }

    public async Task<string> GenerateVisionAsync(
        string model,
        string prompt,
        string imageBase64,
        CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest
        {
            Model = model,
            Stream = false,
            Options = new OllamaOptions
            {
                NumCtx = 8192
            },
            Messages =
            [
                new OllamaMessage
                {
                    Role = "user",
                    Content = prompt,
                    Images = [imageBase64]
                }
            ]
        };

        var result = await SendChatRequestAsync(
            request,
            cancellationToken);

        var content = result.Message.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw CreateEmptyResponseException(model, result);
        }

        return content;
    }

    public async Task<ImageMetadata> GenerateMetadataAsync(
        string model,
        string prompt,
        MetadataPlatform platform,
        CancellationToken cancellationToken = default)
    {
        var allowedCategories = platform switch
        {
            MetadataPlatform.Shutterstock =>
                ShutterstockCategories.All
                    .Cast<string>()
                    .ToArray(),

            MetadataPlatform.AdobeStock =>
                AdobeStockCategories.All
                    .Select(category => category.Name)
                    .ToArray(),

            _ => throw new ArgumentOutOfRangeException(
                nameof(platform),
                platform,
                "Unsupported metadata platform.")
        };

        var request = new OllamaChatRequest
        {
            Model = model,
            Stream = false,
            
            Think = false,

            Format = new
            {
                type = "object",
                properties = new
                {
                    title = new
                    {
                        type = "string"
                    },
                    keywords = new
                    {
                        type = "array",
                        minItems = 20,
                        maxItems = 30,
                        uniqueItems = true,
                        items = new
                        {
                            type = "string"
                        }
                    },
                    category = new
                    {
                        type = "string",
                        @enum = allowedCategories
                    },
                    secondaryCategory = new
                    {
                        type = new[]
                        {
                            "string",
                            "null"
                        },
                        @enum = allowedCategories
                            .Cast<string?>()
                            .Prepend(null)
                            .ToArray()
                    }
                },
                required = new[]
                {
                    "title",
                    "keywords",
                    "category",
                    "secondaryCategory"
                },
                additionalProperties = false
            },

            Options = new OllamaOptions
            {
                NumCtx = 8192,
                Temperature = 0,
                NumPredict = 1024
            },

            Messages =
            [
                new OllamaMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ]
        };

        var result = await SendChatRequestAsync(
            request,
            cancellationToken);

        var content = result.Message.Content;

        System.Diagnostics.Debug.WriteLine(
            $"Ollama model: {model}");

        System.Diagnostics.Debug.WriteLine(
            $"Ollama platform: {platform}");

        System.Diagnostics.Debug.WriteLine(
            $"Ollama response content: [{content}]");

        System.Diagnostics.Debug.WriteLine(
            $"Ollama thinking: [{result.Message.Thinking}]");

        System.Diagnostics.Debug.WriteLine(
            $"Ollama done reason: {result.DoneReason}");

        System.Diagnostics.Debug.WriteLine(
            $"Ollama prompt tokens: {result.PromptEvalCount}");

        System.Diagnostics.Debug.WriteLine(
            $"Ollama generated tokens: {result.EvalCount}");

        if (string.IsNullOrWhiteSpace(content))
        {
            throw CreateEmptyResponseException(model, result);
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<ImageMetadata>(
                content,
                JsonOptions);

            if (metadata is null)
            {
                throw new InvalidOperationException(
                    "Ollama returned JSON, but metadata was empty.");
            }

            return metadata;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Ollama returned invalid metadata JSON. " +
                $"Model: {model}, Platform: {platform}. " +
                $"Response: {content}",
                ex);
        }
    }

    private async Task<OllamaChatResponse> SendChatRequestAsync(
        OllamaChatRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "/api/chat",
            request,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Ollama returned HTTP {(int)response.StatusCode} " +
                $"({response.StatusCode}). " +
                $"Model: {request.Model}. " +
                $"Response: {responseBody}");
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new InvalidOperationException(
                $"Ollama returned an empty HTTP response. " +
                $"Model: {request.Model}. " +
                $"HTTP status: {(int)response.StatusCode}.");
        }

        OllamaChatResponse? result;

        try
        {
            result = JsonSerializer.Deserialize<OllamaChatResponse>(
                responseBody,
                JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Could not parse Ollama HTTP response. " +
                $"Model: {request.Model}. " +
                $"Raw response: {responseBody}",
                ex);
        }

        if (result is null)
        {
            throw new InvalidOperationException(
                $"Ollama returned no response object. " +
                $"Model: {request.Model}. " +
                $"Raw response: {responseBody}");
        }

        return result;
    }

    private static InvalidOperationException CreateEmptyResponseException(
        string model,
        OllamaChatResponse result)
    {
        var thinking = result.Message.Thinking;

        var thinkingInfo = string.IsNullOrWhiteSpace(thinking)
            ? "No thinking content was returned."
            : $"Thinking content length: {thinking.Length} characters.";

        return new InvalidOperationException(
            $"Ollama returned empty message content. " +
            $"Model: {model}. " +
            $"Done reason: {result.DoneReason ?? "unknown"}. " +
            $"Generated tokens: {result.EvalCount?.ToString() ?? "unknown"}. " +
            $"{thinkingInfo}");
    }

    private sealed class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModel> Models { get; init; } = [];
    }

    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("think")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Think { get; init; }

        [JsonPropertyName("format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Format { get; init; }

        [JsonPropertyName("options")]
        public OllamaOptions Options { get; init; } = new();

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; init; } = [];
    }

    private sealed class OllamaOptions
    {
        [JsonPropertyName("num_ctx")]
        public int NumCtx { get; init; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; init; }

        [JsonPropertyName("num_predict")]
        public int NumPredict { get; init; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;

        [JsonPropertyName("thinking")]
        public string? Thinking { get; init; }

        [JsonPropertyName("images")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Images { get; init; }
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage Message { get; init; } = new();

        [JsonPropertyName("done_reason")]
        public string? DoneReason { get; init; }

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; init; }

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; init; }
    }
}
