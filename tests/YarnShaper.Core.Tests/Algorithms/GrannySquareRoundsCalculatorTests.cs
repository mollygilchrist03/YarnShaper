using YarnShaper.Core.Algorithms;
using YarnShaper.Core.Models;

namespace YarnShaper.Core.Tests.Algorithms;

public class GrannySquareRoundsCalculatorTests
{
    private static readonly Gauge WorstedGauge = new(StitchesPerInch: 4, RowsPerInch: 1.5);

    [Fact]
    public void EachRoundHasExactlyFourTimesItsRoundNumberClusters()
    {
        var rows = GrannySquareRoundsCalculator.Calculate(WorstedGauge, new GrannySquareMeasurements(SideLengthInches: 4));

        var ordered = rows.OrderBy(r => r.RowNumber).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var round = i + 1;
            Assert.Equal(round, ordered[i].RowNumber);
            Assert.Equal(4 * round, ordered[i].StitchCount);
        }
    }

    [Fact]
    public void AllRowsAreInTheRoundSectionAndMarkedAsIncreases()
    {
        var rows = GrannySquareRoundsCalculator.Calculate(WorstedGauge, new GrannySquareMeasurements(4));

        Assert.All(rows, r => Assert.Equal(GarmentSection.Round, r.Section));
        Assert.All(rows, r => Assert.Equal(ShapingAction.Increase, r.Action));
    }

    [Fact]
    public void FirstRoundHasFourClusters()
    {
        var rows = GrannySquareRoundsCalculator.Calculate(WorstedGauge, new GrannySquareMeasurements(4));

        Assert.Equal(4, rows.OrderBy(r => r.RowNumber).First().StitchCount);
    }

    [Fact]
    public void TotalRoundsMatchesGaugeRowsForSideLength()
    {
        var measurements = new GrannySquareMeasurements(SideLengthInches: 6);
        var expectedRounds = WorstedGauge.RowsFor(measurements.SideLengthInches);

        var rows = GrannySquareRoundsCalculator.Calculate(WorstedGauge, measurements);

        Assert.Equal(expectedRounds, rows.Count);
    }

    [Fact]
    public void StitchCountGrowsByExactlyFourEachRound()
    {
        var rows = GrannySquareRoundsCalculator.Calculate(WorstedGauge, new GrannySquareMeasurements(6));

        var ordered = rows.OrderBy(r => r.RowNumber).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            Assert.Equal(4, ordered[i].StitchCount - ordered[i - 1].StitchCount);
        }
    }

    [Fact]
    public void ThrowsWhenSideLengthRoundsToLessThanOneRound()
    {
        var coarseGauge = new Gauge(StitchesPerInch: 4, RowsPerInch: 0.1);

        Assert.Throws<ArgumentException>(() =>
            GrannySquareRoundsCalculator.Calculate(coarseGauge, new GrannySquareMeasurements(SideLengthInches: 1)));
    }
}
