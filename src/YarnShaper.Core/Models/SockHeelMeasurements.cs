using System.Runtime.CompilerServices;

namespace YarnShaper.Core.Models;

/// <summary>
/// The one finished measurement a heel flap/turn/gusset needs: the sock's
/// finished foot (and leg) circumference, which the round is split evenly
/// across the heel needle and the instep.
/// </summary>
public sealed record SockHeelMeasurements(double FootCircumferenceInches)
{
    public double FootCircumferenceInches { get; init; } = RequirePositive(FootCircumferenceInches);

    private static double RequirePositive(double value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}
