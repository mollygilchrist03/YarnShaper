using System.Text.Json;
using System.Text.Json.Serialization;
using YarnShaper.Core.Models;

namespace YarnShaper.Core.Tests.AccuracyTests;

/// <summary>
/// A real published pattern's gauge, measurements, and the row-by-row
/// shaping schedule it produces for one or more sections — hand-traced
/// from the pattern text so a calculator's output can be checked against
/// what a real knitter would actually be told to do.
/// </summary>
public sealed record PatternFixture(
    string Name,
    string Size,
    string Notes,
    FixtureGauge Gauge,
    Dictionary<string, double> Measurements,
    Dictionary<string, List<ExpectedRow>> Sections)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static PatternFixture Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "AccuracyTests", "Fixtures", fileName);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PatternFixture>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Fixture '{fileName}' deserialized to null.");
    }

    public Core.Models.Gauge ToGauge() => new(Gauge.StitchesPerInch, Gauge.RowsPerInch);
}

public sealed record FixtureGauge(double StitchesPerInch, double RowsPerInch);

public sealed record ExpectedRow(int Row, int Stitches, ShapingAction Action);
