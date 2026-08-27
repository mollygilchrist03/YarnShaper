using YarnShaper.Core.Algorithms;
using YarnShaper.Core.Models;

namespace YarnShaper.Core.Tests.AccuracyTests;

/// <summary>
/// Compares <see cref="SockHeelShapingCalculator"/> against a real,
/// published pattern (hand-traced into a fixture) rather than only
/// against values the calculator itself produced. See docs/ACCURACY.md
/// for the full writeup of what was checked and what wasn't.
/// </summary>
public class SockHeelAccuracyTests
{
    private static readonly PatternFixture Fixture = PatternFixture.Load("sock-heel-medium-01.json");

    private static IReadOnlyList<ShapingRow> CalculateFixtureRows()
    {
        var gauge = Fixture.ToGauge();
        var measurements = new SockHeelMeasurements(Fixture.Measurements["footCircumferenceInches"]);
        return SockHeelShapingCalculator.Calculate(gauge, measurements);
    }

    [Fact]
    public void HeelFlapMatchesPublishedPattern()
    {
        var actual = CalculateFixtureRows().Where(r => r.Section == GarmentSection.HeelFlap).ToList();
        var diff = RowScheduleComparer.Diff("Heel flap", Fixture.Sections["HeelFlap"], actual);

        Assert.True(diff is null, diff);
    }

    [Fact]
    public void HeelTurnMatchesPublishedPattern()
    {
        var actual = CalculateFixtureRows().Where(r => r.Section == GarmentSection.HeelTurn).ToList();
        var diff = RowScheduleComparer.Diff("Heel turn", Fixture.Sections["HeelTurn"], actual);

        Assert.True(diff is null, diff);
    }

    // The gusset is a documented, known divergence rather than a match: the
    // pattern picks up one extra "ladder" stitch on each side of the join
    // (a standard hole-prevention technique) that SockHeelShapingCalculator
    // doesn't model. This pins the exact size of that gap — 2 fewer
    // stitches at the gusset peak, 1 fewer decrease round — so a future
    // change to the algorithm either closes it deliberately (and this test
    // is updated) or regresses it further (and this test catches that).
    // See docs/ACCURACY.md for the full trace.
    [Fact]
    public void GussetHasTheDocumentedLadderStitchGap()
    {
        var actual = CalculateFixtureRows().Where(r => r.Section == GarmentSection.Gusset).ToList();

        // 14 (heel turn) + 24 (pickup) + 2 (ladder stitches).
        const int patternPeakStitches = 40;
        const int patternDecreaseRounds = 8;

        var calculatorPeakStitches = actual[0].StitchCount;
        var calculatorDecreaseRounds = actual.Count(r => r.Action == ShapingAction.Decrease);

        Assert.Equal(patternPeakStitches - 2, calculatorPeakStitches);
        Assert.Equal(patternDecreaseRounds - 1, calculatorDecreaseRounds);
    }
}
