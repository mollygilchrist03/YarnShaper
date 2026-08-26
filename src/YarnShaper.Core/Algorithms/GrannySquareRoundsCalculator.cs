using YarnShaper.Core.Models;

namespace YarnShaper.Core.Algorithms;

/// <summary>
/// Computes round-by-round growth for a classic granny square: 4 corner
/// clusters (groups of stitches separated by a chain space) on round 1,
/// with 4 more clusters joining every round after.
/// </summary>
/// <remarks>
/// Each of the square's 4 corners gets exactly one cluster every round, and
/// each of its 4 sides gains exactly one additional cluster every round
/// after the first (the side clusters fill in the gap opened up by the
/// previous round's corners moving further apart). That's 4 corner
/// clusters plus 4 &#215; (N - 1) side clusters on round N, which simplifies
/// to a clean 4N — the well-known "add 4 clusters every round" rule found
/// in essentially every granny square pattern. Unlike the raglan or sock
/// heel calculators, there's no distribution problem to solve here: every
/// round adds exactly 4, so <see cref="EvenDistribution"/> isn't needed.
/// The interesting part of this construction is downstream, in how a
/// colorway maps onto a round-based motif rather than a row-based one.
/// </remarks>
public static class GrannySquareRoundsCalculator
{
    private const int ClustersPerRound = 4;

    public static IReadOnlyList<ShapingRow> Calculate(Gauge gauge, GrannySquareMeasurements measurements)
    {
        ArgumentNullException.ThrowIfNull(gauge);
        ArgumentNullException.ThrowIfNull(measurements);

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
            rows.Add(new ShapingRow(round, ClustersPerRound * round, ShapingAction.Increase, GarmentSection.Round));
        }

        return rows;
    }
}
