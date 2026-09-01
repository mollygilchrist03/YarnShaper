using System.Text.Json;
using YarnShaper.Web.Models;

namespace YarnShaper.Web.Services;

/// <summary>
/// Looks up real yarn colorways near a given hex color via the
/// <c>yarn-colorway-proxy</c> Cloudflare Worker (see /workers/yarn-colorway-proxy).
/// The proxy holds the actual RapidAPI credentials, since Yarn Shaper ships
/// as a static WASM bundle with no way to keep a secret client-side.
/// </summary>
public sealed class YarnColorwayService(HttpClient http, string? proxyBaseUrl)
{
    public async Task<YarnColorwayLookupResult> FindMatchesAsync(
        string hexColor, int limit = 8, int threshold = 75, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(proxyBaseUrl))
        {
            return YarnColorwayLookupResult.Failed(
                "Yarn lookup isn't set up for this deployment yet — see workers/yarn-colorway-proxy/README.md.");
        }

        var color = Uri.EscapeDataString(hexColor.TrimStart('#'));
        var url = $"{proxyBaseUrl.TrimEnd('/')}/match?color={color}&limit={limit}&threshold={threshold}";

        try
        {
            using var response = await http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return YarnColorwayLookupResult.Failed($"Yarn lookup service returned {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var parsed = await JsonSerializer.DeserializeAsync(
                stream, YarnColorwayJsonContext.Default.YarnColorwayApiResponse, cancellationToken);
            return YarnColorwayLookupResult.Succeeded(parsed?.Data ?? []);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return YarnColorwayLookupResult.Failed("Couldn't reach the yarn lookup service.");
        }
    }
}

public sealed record YarnColorwayLookupResult(bool IsSuccess, IReadOnlyList<YarnColorwayMatch> Matches, string? ErrorMessage)
{
    public static YarnColorwayLookupResult Succeeded(IReadOnlyList<YarnColorwayMatch> matches) => new(true, matches, null);

    public static YarnColorwayLookupResult Failed(string message) => new(false, [], message);
}
