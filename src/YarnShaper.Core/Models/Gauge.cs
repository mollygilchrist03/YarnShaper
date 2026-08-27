using System.Runtime.CompilerServices;

namespace YarnShaper.Core.Models;

/// <summary>
/// Stitch and row gauge, stored as stitches/rows per inch. Knitting patterns
/// conventionally quote gauge over a 4in or 10cm swatch; use
/// <see cref="FromSwatch"/> to convert one of those into per-inch values.
/// </summary>
public sealed record Gauge(double StitchesPerInch, double RowsPerInch)
{
    public double StitchesPerInch { get; init; } = RequirePositive(StitchesPerInch);
    public double RowsPerInch { get; init; } = RequirePositive(RowsPerInch);

    private static double RequirePositive(double value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }

    public static Gauge FromSwatch(double stitchCount, double rowCount, double swatchWidthInches, double swatchHeightInches)
    {
        if (swatchWidthInches <= 0) throw new ArgumentOutOfRangeException(nameof(swatchWidthInches));
        if (swatchHeightInches <= 0) throw new ArgumentOutOfRangeException(nameof(swatchHeightInches));

        return new Gauge(stitchCount / swatchWidthInches, rowCount / swatchHeightInches);
    }

    public int StitchesFor(double inches) => (int)Math.Round(inches * StitchesPerInch, MidpointRounding.AwayFromZero);

    public int RowsFor(double inches) => (int)Math.Round(inches * RowsPerInch, MidpointRounding.AwayFromZero);
}
