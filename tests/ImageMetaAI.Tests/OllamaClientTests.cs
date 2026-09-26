using System.Net;
using System.Net.Http;
using ImageMetaAI.Services;

namespace ImageMetaAI.Tests;

public class OllamaClientTests
{
    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue_WhenServerReturnsSuccess()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        var result = await client.IsAvailableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsFalse_WhenServerReturnsError()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.InternalServerError);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        var result = await client.IsAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsFalse_WhenServerIsUnavailable()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpRequestException());

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        var result = await client.IsAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsModels_WhenResponseContainsModels()
    {
        const string json = """
            {
                "models": [
                    {
                        "name": "qwen2.5vl:7b"
                    },
                    {
                        "name": "gemma4:26b"
                    }
                ]
            }
            """;

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            json);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        var result = await client.GetModelsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, model => model.Name == "qwen2.5vl:7b");
        Assert.Contains(result, model => model.Name == "gemma4:26b");
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsEmptyList_WhenResponseContainsNoModels()
    {
        const string json = """
            {
                "models": []
            }
            """;

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            json);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        var result = await client.GetModelsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsEmptyList_WhenResponseIsEmpty()
    {
        const string json = """
            {
                "models": []
            }
            """;

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            json);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        var result = await client.GetModelsAsync();

        Assert.Empty(result);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _statusCode;
        private readonly Exception? _exception;
        private readonly string? _content;

        public FakeHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public FakeHttpMessageHandler(
            HttpStatusCode statusCode,
            string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        public FakeHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            var response = new HttpResponseMessage(_statusCode!.Value);

            if (_content is not null)
            {
                response.Content = new StringContent(
                    _content,
                    System.Text.Encoding.UTF8,
                    "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
