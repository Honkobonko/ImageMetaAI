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

    private sealed class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModel> Models { get; init; } = [];
    }
}
