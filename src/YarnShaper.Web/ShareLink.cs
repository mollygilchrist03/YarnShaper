using System.Text;
using System.Text.Json;
using YarnShaper.Web.Models;

namespace YarnShaper.Web;

/// <summary>
/// Packs a calculator's payload into a URL-safe base64 string (and back),
/// so a project can be shared as a link with no server-side state — the
/// whole project lives in the URL itself.
/// </summary>
internal static class ShareLink
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = ProjectJsonContext.Default,
    };

    public static string Encode<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static T? Decode<T>(string encoded)
    {
        var base64 = encoded.Replace('-', '+').Replace('_', '/');
        var padded = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        try
        {
            var bytes = Convert.FromBase64String(padded);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (FormatException)
        {
            return default;
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
