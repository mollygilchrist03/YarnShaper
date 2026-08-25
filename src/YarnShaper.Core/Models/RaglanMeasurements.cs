using System.Runtime.CompilerServices;

namespace YarnShaper.Core.Models;

/// <summary>
/// Finished-garment measurements a top-down raglan yoke is shaped to. These
/// are target dimensions (body measurement + desired ease already applied),
/// not raw body measurements.
/// </summary>
public sealed record RaglanMeasurements(
    double NeckCircumferenceInches,
    double BustCircumferenceInches,
    double UpperArmCircumferenceInches,
    double YokeDepthInches)
{
    public double NeckCircumferenceInches { get; init; } = RequirePositive(NeckCircumferenceInches);
    public double BustCircumferenceInches { get; init; } = RequirePositive(BustCircumferenceInches);
    public double UpperArmCircumferenceInches { get; init; } = RequirePositive(UpperArmCircumferenceInches);
    public double YokeDepthInches { get; init; } = RequirePositive(YokeDepthInches);

    private static double RequirePositive(double value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}
