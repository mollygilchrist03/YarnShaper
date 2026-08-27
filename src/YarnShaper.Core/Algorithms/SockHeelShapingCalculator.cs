using YarnShaper.Core.Models;

namespace YarnShaper.Core.Algorithms;

/// <summary>Which technique the heel is worked with.</summary>
public enum HeelStyle
{
    /// <summary>Square flap worked flat, short-row turn, then gusset stitches picked up and decreased back out.</summary>
    HeelFlapAndGusset,

    /// <summary>No flap — the heel is turned directly with short rows, narrowing to a center point and back out.</summary>
    ShortRowHeel,

    /// <summary>Worked separately after the foot, from stitches picked up around a waste-yarn opening and decreased to a point.</summary>
    AfterthoughtHeel,
}

/// <summary>
/// Computes a sock heel shaping schedule for one of three standard
/// techniques: a heel flap with gusset, a short-row heel, or an
/// afterthought heel.
/// </summary>
/// <remarks>
/// <para>
/// <b>Shared setup.</b> A sock's round splits evenly across two needles:
/// the heel needle and the instep needle. To keep every style's math exact
/// for any input, the total round is rounded to the nearest multiple of 8.
/// </para>
/// <para>
/// <b>Heel flap and gusset</b> (<see cref="HeelStyle.HeelFlapAndGusset"/>).
/// The flap and turn are worked flat on the heel needle's stitches (H)
/// alone; the instep stitches sit untouched until the gusset rejoins them.
/// See the section-building methods below for the flap/turn/gusset math.
/// </para>
/// <para>
/// <b>Short-row heel</b> (<see cref="HeelStyle.ShortRowHeel"/>). No flap:
/// short rows narrow the H heel stitches down to H/2 "active" stitches at
/// the center (wrapping the rest), then widen back out to H, working each
/// wrapped stitch back in. Unlike the flap-and-gusset style, no stitches
/// are picked up — the round returns to its original total with no
/// gusset phase at all, so the schedule tracks the <em>active</em> stitch
/// count narrowing then widening back to H, which also happens to trace
/// the heel's actual hourglass shape.
/// </para>
/// <para>
/// <b>Afterthought heel</b> (<see cref="HeelStyle.AfterthoughtHeel"/>).
/// Worked last, from stitches picked up around a waste-yarn opening (the
/// full round, same count as knitting the foot plain through that round)
/// and decreased like a toe — 4 stitches every other round — down to a
/// small enough count to graft closed.
/// </para>
/// </remarks>
public static class SockHeelShapingCalculator
{
    public static IReadOnlyList<ShapingRow> Calculate(Gauge gauge, SockHeelMeasurements measurements, HeelStyle style = HeelStyle.HeelFlapAndGusset)
    {
        ArgumentNullException.ThrowIfNull(gauge);
        ArgumentNullException.ThrowIfNull(measurements);

        var rawStitches = gauge.StitchesFor(measurements.FootCircumferenceInches);
        var totalStitches = (int)Math.Round(rawStitches / 8.0, MidpointRounding.AwayFromZero) * 8;
        if (totalStitches < 16)
        {
            throw new ArgumentException(
                $"A {measurements.FootCircumferenceInches}in foot circumference at this gauge rounds to only " +
                $"{totalStitches} stitches, too few for a heel.",
                nameof(measurements));
        }

        var heelStitches = totalStitches / 2;

        return style switch
        {
            HeelStyle.HeelFlapAndGusset => BuildHeelFlapAndGusset(heelStitches),
            HeelStyle.ShortRowHeel => BuildShortRowHeel(heelStitches),
            HeelStyle.AfterthoughtHeel => BuildAfterthoughtHeel(totalStitches),
            _ => throw new ArgumentOutOfRangeException(nameof(style)),
        };
    }

    private static List<ShapingRow> BuildHeelFlapAndGusset(int heelStitches)
    {
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

    private static List<ShapingRow> BuildShortRowHeel(int heelStitches)
    {
        var narrowRows = Math.Max(1, heelStitches / 4);
        var rows = new List<ShapingRow>(2 * narrowRows + 1)
        {
            new(1, heelStitches, ShapingAction.None, GarmentSection.ShortRowHeel)
        };

        var stitchCount = heelStitches;
        var rowNumber = 1;
        for (var i = 0; i < narrowRows; i++)
        {
            stitchCount -= 2;
            rows.Add(new ShapingRow(++rowNumber, stitchCount, ShapingAction.Decrease, GarmentSection.ShortRowHeel));
        }

        for (var i = 0; i < narrowRows; i++)
        {
            stitchCount += 2;
            rows.Add(new ShapingRow(++rowNumber, stitchCount, ShapingAction.Increase, GarmentSection.ShortRowHeel));
        }

        return rows;
    }

    private static List<ShapingRow> BuildAfterthoughtHeel(int totalStitches)
    {
        var target = Math.Max(8, totalStitches / 4);
        target -= target % 4;
        if (target < 8) target = 8;

        var rows = new List<ShapingRow> { new(1, totalStitches, ShapingAction.None, GarmentSection.AfterthoughtHeel) };

        var stitchCount = totalStitches;
        var rowNumber = 1;
        var decreaseRounds = (stitchCount - target) / 4;
        for (var d = 1; d <= decreaseRounds; d++)
        {
            stitchCount -= 4;
            rows.Add(new ShapingRow(++rowNumber, stitchCount, ShapingAction.Decrease, GarmentSection.AfterthoughtHeel));

            if (d < decreaseRounds)
            {
                rows.Add(new ShapingRow(++rowNumber, stitchCount, ShapingAction.None, GarmentSection.AfterthoughtHeel));
            }
        }

        return rows;
    }
}
