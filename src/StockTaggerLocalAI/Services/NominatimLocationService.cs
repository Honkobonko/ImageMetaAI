using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using StockTaggerLocalAI.Models;

namespace StockTaggerLocalAI.Services;

public class NominatimLocationService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private readonly Dictionary<string, string?> _sessionResults = new();

    private DateTimeOffset _lastRequestTime = DateTimeOffset.MinValue;

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "StockTaggerLocalAI/1.0");

        return client;
    }

    public async Task<string?> ResolveLocationAsync(
        GpsCoordinates coordinates,
        CancellationToken cancellationToken = default)
    {
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"{coordinates.Latitude:F6},{coordinates.Longitude:F6}");

        if (_sessionResults.TryGetValue(key, out var cachedLocation))
        {
            return cachedLocation;
        }

        await _requestLock.WaitAsync(cancellationToken);

        try
        {
            // Check again after acquiring the lock.
            if (_sessionResults.TryGetValue(key, out cachedLocation))
            {
                return cachedLocation;
            }

            var elapsed = DateTimeOffset.UtcNow - _lastRequestTime;
            var remainingDelay = TimeSpan.FromSeconds(1) - elapsed;

            if (remainingDelay > TimeSpan.Zero)
            {
                await Task.Delay(remainingDelay, cancellationToken);
            }

            var latitude = coordinates.Latitude.ToString(
                CultureInfo.InvariantCulture);

            var longitude = coordinates.Longitude.ToString(
                CultureInfo.InvariantCulture);

            var url =
                "https://nominatim.openstreetmap.org/reverse" +
                $"?format=jsonv2&addressdetails=1&lat={latitude}" +
                $"&lon={longitude}&accept-language=en";

            using var response = await HttpClient.GetAsync(
                url,
                cancellationToken);

            _lastRequestTime = DateTimeOffset.UtcNow;

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken);

            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty(
                    "address",
                    out var address))
            {
                _sessionResults[key] = null;
                return null;
            }

            var parts = new[]
            {
                GetAddressPart(address, "city"),
                GetAddressPart(address, "town"),
                GetAddressPart(address, "village"),
                GetAddressPart(address, "municipality"),
                GetAddressPart(address, "state"),
                GetAddressPart(address, "country")
            };

            var location = string.Join(
                ", ",
                parts
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .Distinct());

            var result = string.IsNullOrWhiteSpace(location)
                ? null
                : location;

            _sessionResults[key] = result;

            return result;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private static string? GetAddressPart(
        JsonElement address,
        string name)
    {
        return address.TryGetProperty(name, out var value)
            ? value.GetString()
            : null;
    }
}
