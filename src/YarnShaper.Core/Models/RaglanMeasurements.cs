using System.Runtime.CompilerServices;

namespace YarnShaper.Core.Models;

/// <summary>
/// Body measurements a top-down or bottom-up raglan yoke is shaped to,
/// plus how much ease to add on top of them. <see cref="EaseInches"/> is
/// added to <see cref="BustCircumferenceInches"/> and
/// <see cref="UpperArmCircumferenceInches"/> to get the actual finished
/// (garment) circumference the calculator shapes to — negative ease gives
/// a closer fit, positive ease gives more room, zero means the finished
/// garment matches the body measurement exactly.
/// </summary>
public sealed record RaglanMeasurements(
    double NeckCircumferenceInches,
    double BustCircumferenceInches,
    double UpperArmCircumferenceInches,
    double YokeDepthInches,
    double EaseInches = 0)
{
    public double NeckCircumferenceInches { get; init; } = RequirePositive(NeckCircumferenceInches);
    public double BustCircumferenceInches { get; init; } = RequirePositive(BustCircumferenceInches);
    public double UpperArmCircumferenceInches { get; init; } = RequirePositive(UpperArmCircumferenceInches);
    public double YokeDepthInches { get; init; } = RequirePositive(YokeDepthInches);
    public double EaseInches { get; init; } = RequireFinishedCircumferencesStayPositive(EaseInches, BustCircumferenceInches, UpperArmCircumferenceInches);

    /// <summary>Body bust circumference plus ease — the actual circumference the back and front are shaped to (split evenly between them).</summary>
    public double FinishedBustCircumferenceInches => BustCircumferenceInches + EaseInches;

    /// <summary>Body upper-arm circumference plus ease — the actual circumference each sleeve is shaped to.</summary>
    public double FinishedUpperArmCircumferenceInches => UpperArmCircumferenceInches + EaseInches;

    private static double RequirePositive(double value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }

    private static double RequireFinishedCircumferencesStayPositive(double easeInches, double bustCircumferenceInches, double upperArmCircumferenceInches)
    {
        if (bustCircumferenceInches + easeInches <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(easeInches), "Ease is negative enough to make the finished bust circumference zero or less.");
        }

        if (upperArmCircumferenceInches + easeInches <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(easeInches), "Ease is negative enough to make the finished upper arm circumference zero or less.");
        }

        return easeInches;
    }
}
