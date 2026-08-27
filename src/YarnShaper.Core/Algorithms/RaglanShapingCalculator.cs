using YarnShaper.Core.Models;

namespace YarnShaper.Core.Algorithms;

/// <summary>Which direction a raglan yoke is worked in.</summary>
public enum RaglanStyle
{
    /// <summary>Cast on at the neck (small), increase every raglan line down to the underarm split (large).</summary>
    TopDown,

    /// <summary>Cast on at the underarm split (large, from bust/upper-arm circumference), decrease every raglan line up to the neck (small).</summary>
    BottomUp,
}

/// <summary>
/// Computes a raglan yoke shaping schedule: how the back, front, and two
/// sleeves grow (top-down) or shrink (bottom-up), round by round, between
/// the neck and the underarm split.
/// </summary>
/// <remarks>
/// <para>
/// A raglan yoke is built around 4 diagonal "raglan lines" — back/sleeve,
/// sleeve/front, front/sleeve, sleeve/back — each of which grows by
/// working an increase immediately on both sides of its marker. That's 2
/// new stitches per raglan line, 4 lines, so every shaping round adds (or
/// removes, bottom-up) 2 stitches to <em>each</em> of the 4 sections (back,
/// front, and both sleeves) — 8 stitches total per round. That invariant
/// (always ±2 per section per shaping round) is what makes the section
/// stitch counts track cleanly against target circumferences below.
/// </para>
/// <para>
/// The neck's stitch count is split across the 4 sections proportionally —
/// back and front each get a larger share than a sleeve, since a sleeve's
/// circumference at the shoulder is narrower than half the bust. From
/// there, each section needs a different number of shaping rounds to reach
/// its target circumference at the underarm (sleeves usually need fewer
/// than the body), but all sections share the same yoke depth (row
/// budget). Rather than clumping a section's shaping at one end of the
/// yoke and leaving it idle for the rest, each section's shaping is spread
/// evenly across the full row budget via <see cref="EvenDistribution"/> —
/// matching how real raglan patterns space "increase every other round"
/// (or every third, etc.) rather than front-loading it.
/// </para>
/// <para>
/// <see cref="RaglanStyle.BottomUp"/> is the same construction worked in
/// the opposite direction: cast on at the underarm split (computed from
/// the finished bust/upper-arm circumferences) and decrease down to the
/// neck's proportional split. The row-building logic
/// (<see cref="BuildSection"/>) is shared between both styles — only the
/// start/target stitch counts and the shaping direction (+2 vs -2 per
/// round) differ.
/// </para>
/// </remarks>
public static class RaglanShapingCalculator
{
    private const double BodyShare = 0.30;
    private const double SleeveShare = 0.20;

    public static IReadOnlyList<ShapingRow> Calculate(Gauge gauge, RaglanMeasurements measurements, RaglanStyle style = RaglanStyle.TopDown)
    {
        ArgumentNullException.ThrowIfNull(gauge);
        ArgumentNullException.ThrowIfNull(measurements);

        var castOnStitches = gauge.StitchesFor(measurements.NeckCircumferenceInches);
        var (neckBack, neckFront, neckSleeve) = SplitCastOn(castOnStitches);

        var underarmBack = gauge.StitchesFor(measurements.FinishedBustCircumferenceInches / 2);
        var underarmFront = underarmBack;
        var underarmSleeve = gauge.StitchesFor(measurements.FinishedUpperArmCircumferenceInches);

        var totalYokeRows = gauge.RowsFor(measurements.YokeDepthInches);
        if (totalYokeRows < 2)
        {
            throw new ArgumentException(
                $"Yoke depth of {measurements.YokeDepthInches}in is too short at this row gauge to fit any shaping rows.",
                nameof(measurements));
        }

        // Row 1 is the cast-on round itself.
        var availableShapingRows = totalYokeRows - 1;

        var (startBack, targetBack, startFront, targetFront, startSleeve, targetSleeve, delta) = style switch
        {
            RaglanStyle.TopDown => (neckBack, underarmBack, neckFront, underarmFront, neckSleeve, underarmSleeve, 2),
            RaglanStyle.BottomUp => (underarmBack, neckBack, underarmFront, neckFront, underarmSleeve, neckSleeve, -2),
            _ => throw new ArgumentOutOfRangeException(nameof(style)),
        };

        var backRounds = ShapingRoundsNeeded(startBack, targetBack, delta);
        var frontRounds = ShapingRoundsNeeded(startFront, targetFront, delta);
        var sleeveRounds = ShapingRoundsNeeded(startSleeve, targetSleeve, delta);
        var maxRounds = Math.Max(backRounds, Math.Max(frontRounds, sleeveRounds));

        if (maxRounds > availableShapingRows)
        {
            throw new ArgumentException(
                $"Yoke depth of {measurements.YokeDepthInches}in only allows {availableShapingRows} shaping rows, " +
                $"but reaching the target circumferences needs {maxRounds}.",
                nameof(measurements));
        }

        var rows = new List<ShapingRow>();
        rows.AddRange(BuildSection(GarmentSection.Back, startBack, backRounds, availableShapingRows, delta));
        rows.AddRange(BuildSection(GarmentSection.Front, startFront, frontRounds, availableShapingRows, delta));
        rows.AddRange(BuildSection(GarmentSection.LeftSleeve, startSleeve, sleeveRounds, availableShapingRows, delta));
        rows.AddRange(BuildSection(GarmentSection.RightSleeve, startSleeve, sleeveRounds, availableShapingRows, delta));
        return rows;
    }

    private static (int Back, int Front, int Sleeve) SplitCastOn(int castOnStitches)
    {
        var back = (int)Math.Round(castOnStitches * BodyShare, MidpointRounding.AwayFromZero);
        var front = back;
        var sleeve = (int)Math.Round(castOnStitches * SleeveShare, MidpointRounding.AwayFromZero);

        // Any rounding remainder (the split won't divide the cast-on exactly) goes to the back.
        var remainder = castOnStitches - (back + front + sleeve * 2);
        back += remainder;

        return (back, front, sleeve);
    }

    private static int ShapingRoundsNeeded(int start, int target, int delta)
    {
        if (delta > 0)
        {
            return target <= start ? 0 : (int)Math.Ceiling((target - start) / 2.0);
        }

        return target >= start ? 0 : (int)Math.Ceiling((start - target) / 2.0);
    }

    private static List<ShapingRow> BuildSection(GarmentSection section, int startStitches, int shapingRoundCount, int availableShapingRows, int delta)
    {
        var flags = EvenDistribution.Distribute(availableShapingRows, shapingRoundCount);
        var action = delta > 0 ? ShapingAction.Increase : ShapingAction.Decrease;

        var rows = new List<ShapingRow>(availableShapingRows + 1)
        {
            new(1, startStitches, ShapingAction.None, section)
        };

        var stitchCount = startStitches;
        for (var i = 0; i < flags.Length; i++)
        {
            var rowNumber = i + 2;
            if (flags[i])
            {
                stitchCount += delta;
                rows.Add(new ShapingRow(rowNumber, stitchCount, action, section));
            }
            else
            {
                rows.Add(new ShapingRow(rowNumber, stitchCount, ShapingAction.None, section));
            }
        }

        return rows;
    }
}
