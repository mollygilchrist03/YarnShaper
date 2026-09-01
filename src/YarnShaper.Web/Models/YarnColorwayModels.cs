using System.Text.Json.Serialization;

namespace YarnShaper.Web.Models;

public sealed record YarnColorwayMatch(
    string Name,
    string Hex,
    string YarnName,
    string BrandName,
    string? Href,
    int? PercentMatch);

internal sealed record YarnColorwayApiResponse(List<YarnColorwayMatch> Data);

// Blazor WebAssembly publishes with IL trimming on, which can silently drop
// the reflection metadata System.Text.Json needs for plain POCOs. Source
// generation sidesteps that entirely, so the trimmed Release build keeps
// deserializing colorway matches correctly, not just `dotnet run`.
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(YarnColorwayApiResponse))]
internal sealed partial class YarnColorwayJsonContext : JsonSerializerContext
{
}
