using YarnShaper.Core.Models;

namespace YarnShaper.Core.Yardage;

/// <summary>
/// Estimates yarn consumption per color from a shaping schedule and gauge.
/// </summary>
/// <remarks>
/// There's no way to derive exact yardage from stitch counts alone — the
/// only reliable method is weighing a swatch of the actual yarn. This uses
/// a widely-cited rule of thumb instead: a stockinette stitch consumes
/// roughly <see cref="DefaultYarnPerStitchWidthMultiplier"/> times its own
/// width in yarn, since the working yarn loops around itself to form the
/// stitch. A stitch's width is already implied by the gauge the calculator
/// was given (1 / stitches-per-inch), so this doesn't need a separate yarn
/// weight input — it reuses the same <see cref="Gauge"/> already on
/// screen. The multiplier is exposed as a parameter specifically so it can
/// be recalibrated against a real swatch instead of trusted blindly.
/// </remarks>
public static class YardageEstimator
{
    public const double DefaultYarnPerStitchWidthMultiplier = 5.0;

    private const double InchesPerYard = 36.0;

    public static IReadOnlyDictionary<string, double> EstimateYardageByColor(
        IEnumerable<(int StitchCount, string Color)> rows,
        Gauge gauge,
        double yarnPerStitchWidthMultiplier = DefaultYarnPerStitchWidthMultiplier)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(gauge);
        if (yarnPerStitchWidthMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(yarnPerStitchWidthMultiplier));
        }

        var inchesPerStitch = yarnPerStitchWidthMultiplier / gauge.StitchesPerInch;
        var totals = new Dictionary<string, double>();

        foreach (var (stitchCount, color) in rows)
        {
            var yards = stitchCount * inchesPerStitch / InchesPerYard;
            totals[color] = totals.GetValueOrDefault(color) + yards;
        }

        return totals;
    }
}
