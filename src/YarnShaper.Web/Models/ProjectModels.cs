using System.Text.Json.Serialization;

namespace YarnShaper.Web.Models;

/// <summary>Metadata-only record kept in the shared project index so "My Projects" can list every saved project without loading each full payload.</summary>
public sealed record ProjectIndexEntry(string Id, string Name, string CalculatorKind, DateTimeOffset SavedAtUtc);

public sealed record StripeDto(string Color, int RowCount);

public sealed record RaglanProjectPayload(
    double StitchesPerInch,
    double RowsPerInch,
    double NeckCircumferenceInches,
    double BustCircumferenceInches,
    double UpperArmCircumferenceInches,
    double YokeDepthInches,
    List<StripeDto> Colorway);

public sealed record SockHeelProjectPayload(
    double StitchesPerInch,
    double RowsPerInch,
    double FootCircumferenceInches,
    List<StripeDto> Colorway);

public sealed record GrannySquareProjectPayload(
    double RoundsPerInch,
    double SideLengthInches,
    List<StripeDto> Colorway);

// Blazor WebAssembly publishes with IL trimming on, which can silently drop
// the reflection metadata System.Text.Json needs for plain POCOs. Source
// generation sidesteps that entirely, so saved projects and shareable links
// keep working in the trimmed Release build, not just `dotnet run`.
[JsonSerializable(typeof(List<ProjectIndexEntry>))]
[JsonSerializable(typeof(RaglanProjectPayload))]
[JsonSerializable(typeof(SockHeelProjectPayload))]
[JsonSerializable(typeof(GrannySquareProjectPayload))]
internal sealed partial class ProjectJsonContext : JsonSerializerContext
{
}
