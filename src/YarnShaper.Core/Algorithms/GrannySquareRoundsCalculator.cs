using YarnShaper.Core.Models;

namespace YarnShaper.Core.Algorithms;

/// <summary>
/// Computes round-by-round growth for a classic granny motif: N corner
/// clusters (groups of stitches separated by a chain space) on round 1,
/// with N more clusters joining every round after — 4 corners for the
/// familiar granny square, but the same rule generalizes cleanly to
/// hexagons, triangles, or any other corner count.
/// </summary>
/// <remarks>
/// Each of the motif's N corners gets exactly one cluster every round, and
/// each of its N sides gains exactly one additional cluster every round
/// after the first (the side clusters fill in the gap opened up by the
/// previous round's corners moving further apart). That's N corner
/// clusters plus N &#215; (round - 1) side clusters on a given round, which
/// simplifies to a clean N &#215; round — the well-known "add N clusters every
/// round" rule found in essentially every granny square (N=4) or granny
/// hexagon (N=6) pattern. Unlike the raglan or sock heel calculators,
/// there's no distribution problem to solve here: every round adds exactly
/// N, so <see cref="EvenDistribution"/> isn't needed. The interesting part
/// of this construction is downstream, in how a colorway maps onto a
/// round-based motif rather than a row-based one.
/// </remarks>
public static class GrannySquareRoundsCalculator
{
    public static IReadOnlyList<ShapingRow> Calculate(Gauge gauge, GrannySquareMeasurements measurements, int cornerCount = 4)
    {
        ArgumentNullException.ThrowIfNull(gauge);
        ArgumentNullException.ThrowIfNull(measurements);
        if (cornerCount < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(cornerCount), "A motif needs at least 3 corners.");
        }

        var totalRounds = gauge.RowsFor(measurements.SideLengthInches);
        if (totalRounds < 1)
        {
            throw new ArgumentException(
                $"A {measurements.SideLengthInches}in side length at this gauge rounds to fewer than 1 round.",
                nameof(measurements));
        }

        var rows = new List<ShapingRow>(totalRounds);
        for (var round = 1; round <= totalRounds; round++)
        {
            rows.Add(new ShapingRow(round, cornerCount * round, ShapingAction.Increase, GarmentSection.Round));
        }

        return rows;
    }
}
