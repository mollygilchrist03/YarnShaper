using System.Runtime.CompilerServices;

namespace YarnShaper.Core.Models;

/// <summary>
/// The one finished measurement a granny square needs: the side length it
/// should grow to. Gauge for a round-based motif is naturally expressed as
/// rounds per inch of growth, so this reuses <see cref="Gauge.RowsPerInch"/>
/// for that rather than introducing a separate unit.
/// </summary>
public sealed record GrannySquareMeasurements(double SideLengthInches)
{
    public double SideLengthInches { get; init; } = RequirePositive(SideLengthInches);

    private static double RequirePositive(double value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}
