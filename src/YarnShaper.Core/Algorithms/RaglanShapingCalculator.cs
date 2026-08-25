using YarnShaper.Core.Models;

namespace YarnShaper.Core.Algorithms;

/// <summary>
/// Computes a top-down raglan yoke shaping schedule: how the back, front,
/// and two sleeves grow, round by round, from the neck cast-on to the
/// underarm split.
/// </summary>
/// <remarks>
/// <para>
/// A raglan yoke is built around 4 diagonal "raglan lines" — back/sleeve,
/// sleeve/front, front/sleeve, sleeve/back — each of which grows by
/// working an increase immediately on both sides of its marker. That's 2
/// new stitches per raglan line, 4 lines, so every increase round adds 2
/// stitches to <em>each</em> of the 4 sections (back, front, and both
/// sleeves) — 8 stitches total per round. That invariant (always +2 per
/// section per increase round) is what makes the section stitch counts
/// track cleanly against target circumferences below.
/// </para>
/// <para>
/// The neck cast-on is split across the 4 sections proportionally — back
/// and front each get a larger share than a sleeve, since a sleeve's
/// circumference at the shoulder is narrower than half the bust. From
/// there, each section needs a different number of increase rounds to
/// reach its target circumference (sleeves usually need fewer than the
/// body), but all sections share the same yoke depth (row budget). Rather
/// than clumping a section's increases at the top of the yoke and leaving
/// it idle for the rest, each section's increases are spread evenly across
/// the full row budget via <see cref="EvenDistribution"/> — matching how
/// real raglan patterns space "increase every other round" (or every third,
/// etc.) rather than front-loading the shaping.
/// </para>
/// </remarks>
public static class RaglanShapingCalculator
{
    private const double BodyShare = 0.30;
    private const double SleeveShare = 0.20;

    public static IReadOnlyList<ShapingRow> Calculate(Gauge gauge, RaglanMeasurements measurements)
    {
        ArgumentNullException.ThrowIfNull(gauge);
        ArgumentNullException.ThrowIfNull(measurements);

        var castOnStitches = gauge.StitchesFor(measurements.NeckCircumferenceInches);
        var (backStart, frontStart, sleeveStart) = SplitCastOn(castOnStitches);

        var backTarget = gauge.StitchesFor(measurements.BustCircumferenceInches / 2);
        var frontTarget = backTarget;
        var sleeveTarget = gauge.StitchesFor(measurements.UpperArmCircumferenceInches);

        var totalYokeRows = gauge.RowsFor(measurements.YokeDepthInches);
        if (totalYokeRows < 2)
        {
            throw new ArgumentException(
                $"Yoke depth of {measurements.YokeDepthInches}in is too short at this row gauge to fit any shaping rows.",
                nameof(measurements));
        }

        var availableIncreaseRows = totalYokeRows - 1; // row 1 is the cast-on round itself

        var backIncreases = IncreaseRoundsNeeded(backStart, backTarget);
        var frontIncreases = IncreaseRoundsNeeded(frontStart, frontTarget);
        var sleeveIncreases = IncreaseRoundsNeeded(sleeveStart, sleeveTarget);
        var maxIncreases = Math.Max(backIncreases, Math.Max(frontIncreases, sleeveIncreases));

        if (maxIncreases > availableIncreaseRows)
        {
            throw new ArgumentException(
                $"Yoke depth of {measurements.YokeDepthInches}in only allows {availableIncreaseRows} shaping rows, " +
                $"but reaching the target circumferences needs {maxIncreases}.",
                nameof(measurements));
        }

        var rows = new List<ShapingRow>();
        rows.AddRange(BuildSection(GarmentSection.Back, backStart, backIncreases, availableIncreaseRows));
        rows.AddRange(BuildSection(GarmentSection.Front, frontStart, frontIncreases, availableIncreaseRows));
        rows.AddRange(BuildSection(GarmentSection.LeftSleeve, sleeveStart, sleeveIncreases, availableIncreaseRows));
        rows.AddRange(BuildSection(GarmentSection.RightSleeve, sleeveStart, sleeveIncreases, availableIncreaseRows));
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

    private static int IncreaseRoundsNeeded(int start, int target)
    {
        if (target <= start) return 0;

        // Each increase round adds 2 stitches to the section (see class remarks).
        return (int)Math.Ceiling((target - start) / 2.0);
    }

    private static List<ShapingRow> BuildSection(GarmentSection section, int startStitches, int increaseCount, int availableIncreaseRows)
    {
        var flags = EvenDistribution.Distribute(availableIncreaseRows, increaseCount);

        var rows = new List<ShapingRow>(availableIncreaseRows + 1)
        {
            new(1, startStitches, ShapingAction.None, section)
        };

        var stitchCount = startStitches;
        for (var i = 0; i < flags.Length; i++)
        {
            var rowNumber = i + 2;
            if (flags[i])
            {
                stitchCount += 2;
                rows.Add(new ShapingRow(rowNumber, stitchCount, ShapingAction.Increase, section));
            }
            else
            {
                rows.Add(new ShapingRow(rowNumber, stitchCount, ShapingAction.None, section));
            }
        }

        return rows;
    }
}
