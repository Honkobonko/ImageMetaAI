using System.Net;
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

    [Fact]
    public async Task GenerateVisionAsync_ReturnsContent_WhenResponseIsSuccessful()
    {
        const string json = """
            {
                "message": {
                    "role": "assistant",
                    "content": "A mountain landscape."
                }
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

        var result = await client.GenerateVisionAsync(
            "test-vision-model",
            "Analyze this image.",
            "base64-image-data");

        Assert.Equal(
            "A mountain landscape.",
            result);
    }

    [Fact]
    public async Task GenerateVisionAsync_Throws_WhenResponseIsNotSuccessful()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.InternalServerError);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GenerateVisionAsync(
                "test-vision-model",
                "Analyze this image.",
                "base64-image-data"));
    }

    [Fact]
    public async Task GenerateVisionAsync_SendsExpectedContextSize()
    {
        const string json = """
            {
                "message": {
                    "role": "assistant",
                    "content": "test"
                }
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

        await client.GenerateVisionAsync(
            "test-vision-model",
            "Analyze this image.",
            "base64-image-data");

        Assert.NotNull(handler.LastRequestContent);

        Assert.Contains(
            "\"num_ctx\":8192",
            handler.LastRequestContent);
    }

    [Fact]
    public async Task GenerateMetadataAsync_ReturnsMetadata_WhenResponseIsValid()
    {
        const string json = """
            {
                "message": {
                    "role": "assistant",
                    "content": "{\"title\":\"Freshly baked bread\",\"keywords\":[\"bread\",\"bakery\",\"food\"]}"
                }
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

        var result = await client.GenerateMetadataAsync(
            "test-metadata-model",
            "Create metadata from this description.");

        Assert.Equal(
            "Freshly baked bread",
            result.Title);

        Assert.Equal(
            ["bread", "bakery", "food"],
            result.Keywords);
    }

    [Fact]
    public async Task GenerateMetadataAsync_Throws_WhenResponseIsNotSuccessful()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.InternalServerError);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GenerateMetadataAsync(
                "test-metadata-model",
                "Create metadata from this description."));
    }

    [Fact]
    public async Task GenerateMetadataAsync_Throws_WhenMetadataJsonIsInvalid()
    {
        const string json = """
            {
                "message": {
                    "role": "assistant",
                    "content": "This is not valid JSON."
                }
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

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GenerateMetadataAsync(
                "test-metadata-model",
                "Create metadata from this description."));
    }

    [Fact]
    public async Task GenerateMetadataAsync_DoesNotSendImages()
    {
        const string json = """
            {
                "message": {
                    "role": "assistant",
                    "content": "{\"title\":\"Freshly baked bread\",\"keywords\":[\"bread\"]}"
                }
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

        await client.GenerateMetadataAsync(
            "test-metadata-model",
            "Create metadata from this description.");

        Assert.NotNull(handler.LastRequestContent);

        Assert.DoesNotContain(
            "\"images\"",
            handler.LastRequestContent);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _statusCode;
        private readonly Exception? _exception;
        private readonly string? _content;

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestContent { get; private set; }

        public FakeHttpMessageHandler(
            HttpStatusCode statusCode)
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

        public FakeHttpMessageHandler(
            Exception exception)
        {
            _exception = exception;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            LastRequestContent = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(
                    cancellationToken);

            if (_exception is not null)
            {
                throw _exception;
            }

            var response = new HttpResponseMessage(
                _statusCode!.Value);

            if (_content is not null)
            {
                response.Content = new StringContent(
                    _content,
                    System.Text.Encoding.UTF8,
                    "application/json");
            }

            return response;
        }
    }
}
