using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ImageMetaAI.Models;

namespace ImageMetaAI.Services;

public class OllamaClient : IOllamaClient
{
    private readonly HttpClient _httpClient;

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

        if (response is null)
        {
            return [];
        }

        return response.Models;
    }

    public async Task<string> GenerateAsync(
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

        var response = await _httpClient.PostAsJsonAsync(
            "/api/chat",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Ollama returned {(int)response.StatusCode} " +
                $"{response.StatusCode}: {errorContent}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<OllamaChatResponse>(
                cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "Ollama returned an empty response.");
        }

        return result.Message.Content;
    }

    private sealed class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModel> Models { get; init; } = [];
    }

    private sealed class OllamaChatRequest
    {
        public string Model { get; init; } = string.Empty;

        public bool Stream { get; init; }

        public OllamaOptions Options { get; init; } = new();

        public List<OllamaMessage> Messages { get; init; } = [];
    }

    private sealed class OllamaOptions
    {
        [JsonPropertyName("num_ctx")]
        public int NumCtx { get; init; }
    }

    private sealed class OllamaMessage
    {
        public string Role { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;

        public List<string> Images { get; init; } = [];
    }

    private sealed class OllamaChatResponse
    {
        public OllamaMessage Message { get; init; } = new();
    }
}
