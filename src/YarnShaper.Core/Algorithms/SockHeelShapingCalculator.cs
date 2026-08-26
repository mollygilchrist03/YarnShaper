using YarnShaper.Core.Models;

namespace YarnShaper.Core.Algorithms;

/// <summary>
/// Computes a standard short-row sock heel: a square heel flap, a
/// short-row heel turn, and the gusset stitches picked up along the flap
/// and decreased back out as the round rejoins the instep.
/// </summary>
/// <remarks>
/// <para>
/// <b>Setup.</b> A sock's round splits evenly across two needles: the heel
/// needle and the instep needle. The heel flap and turn are worked flat on
/// the heel needle's stitches alone; the instep stitches sit untouched
/// until the gusset rejoins them. To keep the gusset math (below) exact for
/// any input, the total round is rounded to the nearest multiple of 8 —
/// the simplifying assumption that plays the same role here as the raglan
/// calculator's proportional cast-on split.
/// </para>
/// <para>
/// <b>Heel flap.</b> Worked back and forth on the heel stitches (H) for H
/// rows, so the flap comes out square — the standard convention, since a
/// flap much longer or shorter than it is wide distorts the heel pocket.
/// </para>
/// <para>
/// <b>Heel turn.</b> Short rows that decrease the working stitches by one
/// every row until only H/2 + 2 stitches remain — the classic "turn the
/// heel" instruction found in most sock patterns. Starting from
/// K(H/2 + 1) and working one stitch further before each decrease, the
/// last row is worked exactly when H/2 - 2 decrease rows have happened,
/// which is why that's the row count used here rather than an iterative
/// search.
/// </para>
/// <para>
/// <b>Gusset.</b> Picking up 1 stitch per slipped edge along each side of
/// the flap (H/2 per side, since the flap slips every other row) adds
/// 3H/2 + 2 stitches to the H/2 + 2 left after the turn. Decreasing 2
/// stitches per round (1 at each gusset/instep join) brings that back down
/// to exactly H — the original heel-needle count — after (H/2 + 2) / 2
/// decrease rounds, alternating with plain rounds between them. That
/// division is exact precisely because the round was rounded to a
/// multiple of 8 during setup.
/// </para>
/// </remarks>
public static class SockHeelShapingCalculator
{
    public static IReadOnlyList<ShapingRow> Calculate(Gauge gauge, SockHeelMeasurements measurements)
    {
        ArgumentNullException.ThrowIfNull(gauge);
        ArgumentNullException.ThrowIfNull(measurements);

        var rawStitches = gauge.StitchesFor(measurements.FootCircumferenceInches);
        var totalStitches = (int)Math.Round(rawStitches / 8.0, MidpointRounding.AwayFromZero) * 8;
        if (totalStitches < 16)
        {
            throw new ArgumentException(
                $"A {measurements.FootCircumferenceInches}in foot circumference at this gauge rounds to only " +
                $"{totalStitches} stitches, too few for a heel flap and turn.",
                nameof(measurements));
        }

        var heelStitches = totalStitches / 2;

        var rows = new List<ShapingRow>();
        rows.AddRange(BuildHeelFlap(heelStitches));

        var turnRows = BuildHeelTurn(heelStitches);
        rows.AddRange(turnRows);
        var heelTurnStitches = turnRows[^1].StitchCount;

        rows.AddRange(BuildGusset(heelStitches, heelTurnStitches));
        return rows;
    }

    private static List<ShapingRow> BuildHeelFlap(int heelStitches)
    {
        var rows = new List<ShapingRow>(heelStitches);
        for (var rowNumber = 1; rowNumber <= heelStitches; rowNumber++)
        {
            rows.Add(new ShapingRow(rowNumber, heelStitches, ShapingAction.None, GarmentSection.HeelFlap));
        }

        return rows;
    }

    private static List<ShapingRow> BuildHeelTurn(int heelStitches)
    {
        var turnRows = heelStitches / 2 - 2;
        var rows = new List<ShapingRow>(turnRows);
        for (var rowNumber = 1; rowNumber <= turnRows; rowNumber++)
        {
            var stitchCount = heelStitches - rowNumber;
            rows.Add(new ShapingRow(rowNumber, stitchCount, ShapingAction.Decrease, GarmentSection.HeelTurn));
        }

        return rows;
    }

    private static List<ShapingRow> BuildGusset(int heelStitches, int heelTurnStitches)
    {
        var pickupPerSide = heelStitches / 2;
        var stitchCount = heelTurnStitches + 2 * pickupPerSide;
        var decreaseRounds = (stitchCount - heelStitches) / 2;

        var rows = new List<ShapingRow> { new(1, stitchCount, ShapingAction.Increase, GarmentSection.Gusset) };

        var rowNumber = 1;
        for (var d = 1; d <= decreaseRounds; d++)
        {
            stitchCount -= 2;
            rows.Add(new ShapingRow(++rowNumber, stitchCount, ShapingAction.Decrease, GarmentSection.Gusset));

            if (d < decreaseRounds)
            {
                rows.Add(new ShapingRow(++rowNumber, stitchCount, ShapingAction.None, GarmentSection.Gusset));
            }
        }

        return rows;
    }
}
