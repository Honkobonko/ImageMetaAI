using System.Net;
using ImageMetaAI.Models;
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
            "Create metadata from this description.",
            MetadataPlatform.Shutterstock);

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
                "Create metadata from this description.",
                MetadataPlatform.Shutterstock));
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
                "Create metadata from this description.",
                MetadataPlatform.Shutterstock));
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
            "Create metadata from this description.",
            MetadataPlatform.Shutterstock);

        Assert.NotNull(handler.LastRequestContent);

        Assert.DoesNotContain(
            "\"images\"",
            handler.LastRequestContent);
    }

    
    [Fact]
    public async Task GenerateMetadataAsync_UsesAdobeStockCategories_WhenPlatformIsAdobeStock()
    {
        const string json = """
            {
                "message": {
                    "role": "assistant",
                    "content": "{\"title\":\"Mountain landscape\",\"keywords\":[\"mountain\"]}"
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
            "Create Adobe Stock metadata.",
            MetadataPlatform.AdobeStock);

        Assert.NotNull(handler.LastRequestContent);

        using var document = System.Text.Json.JsonDocument.Parse(
            handler.LastRequestContent);

        var format = document.RootElement.GetProperty("format");
        var properties = format.GetProperty("properties");
        var category = properties.GetProperty("category");
        var categoryEnum = category.GetProperty("enum");

        var actualCategories = categoryEnum
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        var expectedCategories = AdobeStockCategories.All
            .Select(item => item.Name)
            .ToArray();

        Assert.Equal(expectedCategories, actualCategories);
        Assert.Contains("Landscapes", actualCategories);
        Assert.DoesNotContain("Nature", actualCategories);
    }

    [Fact]
    public async Task GenerateMetadataAsync_UsesShutterstockCategories_WhenPlatformIsShutterstock()
    {
        const string json = """
            {
                "message": {
                    "role": "assistant",
                    "content": "{\"title\":\"Mountain landscape\",\"keywords\":[\"mountain\"]}"
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
            "Create Shutterstock metadata.",
            MetadataPlatform.Shutterstock);

        Assert.NotNull(handler.LastRequestContent);

        using var document = System.Text.Json.JsonDocument.Parse(
            handler.LastRequestContent);

        var properties = document.RootElement
            .GetProperty("format")
            .GetProperty("properties");

        var actualCategories = properties
            .GetProperty("category")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        var expectedCategories = ShutterstockCategories.All
            .Cast<string>()
            .ToArray();

        Assert.Equal(expectedCategories, actualCategories);
        Assert.Contains("Nature", actualCategories);
    }

    [Fact]
    public async Task GenerateMetadataAsync_Throws_WhenPlatformIsUnsupported()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """{"message":{"role":"assistant","content":"{}"}}""");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.GenerateMetadataAsync(
                "test-metadata-model",
                "Create metadata.",
                (MetadataPlatform)999));

        Assert.Null(handler.LastRequest);
    }


    [Fact]
    public async Task GenerateMetadataAsync_AllowsNullSecondaryCategory_ForAdobeStock()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """{"message":{"role":"assistant","content":"{\"title\":\"Mountain landscape\",\"keywords\":[\"mountain\"]}"}}""");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        await client.GenerateMetadataAsync(
            "test-metadata-model",
            "Create Adobe Stock metadata.",
            MetadataPlatform.AdobeStock);

        using var document = System.Text.Json.JsonDocument.Parse(
            handler.LastRequestContent!);

        var secondaryCategory = document.RootElement
            .GetProperty("format")
            .GetProperty("properties")
            .GetProperty("secondaryCategory");

        Assert.Contains(
            secondaryCategory.GetProperty("type")
                .EnumerateArray()
                .Select(item => item.GetString()),
            type => type == "null");

        var allowedValues = secondaryCategory
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.ValueKind ==
                System.Text.Json.JsonValueKind.Null
                    ? null
                    : item.GetString())
            .ToArray();

        Assert.Contains(null, allowedValues);
        Assert.Contains("Landscapes", allowedValues);
        Assert.DoesNotContain("Nature", allowedValues);
    }

    [Fact]
    public async Task GenerateMetadataAsync_AllowsNullSecondaryCategory_ForShutterstock()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """{"message":{"role":"assistant","content":"{\"title\":\"Mountain landscape\",\"keywords\":[\"mountain\"]}"}}""");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        await client.GenerateMetadataAsync(
            "test-metadata-model",
            "Create Shutterstock metadata.",
            MetadataPlatform.Shutterstock);

        using var document = System.Text.Json.JsonDocument.Parse(
            handler.LastRequestContent!);

        var secondaryCategory = document.RootElement
            .GetProperty("format")
            .GetProperty("properties")
            .GetProperty("secondaryCategory");

        var allowedValues = secondaryCategory
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.ValueKind ==
                System.Text.Json.JsonValueKind.Null
                    ? null
                    : item.GetString())
            .ToArray();

        Assert.Contains(null, allowedValues);
        Assert.Contains("Nature", allowedValues);
    }

    [Fact]
    public async Task GenerateMetadataAsync_SendsExpectedRequestOptions()
    {
        const string model = "test-metadata-model";
        const string prompt = "Create stock metadata.";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """{"message":{"role":"assistant","content":"{\"title\":\"Mountain landscape\",\"keywords\":[\"mountain\"]}"}}""");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        var client = new OllamaClient(httpClient);

        await client.GenerateMetadataAsync(
            model,
            prompt,
            MetadataPlatform.AdobeStock);

        using var document = System.Text.Json.JsonDocument.Parse(
            handler.LastRequestContent!);

        var root = document.RootElement;

        Assert.Equal(
            model,
            root.GetProperty("model").GetString());

        Assert.False(root.GetProperty("stream").GetBoolean());

        Assert.Equal(
            prompt,
            root.GetProperty("messages")[0]
                .GetProperty("content")
                .GetString());

        var options = root.GetProperty("options");

        Assert.Equal(8192, options.GetProperty("num_ctx").GetInt32());
        Assert.Equal(0, options.GetProperty("temperature").GetDouble());
        Assert.Equal(1024, options.GetProperty("num_predict").GetInt32());
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
