using YarnShaper.Core.Algorithms;
using YarnShaper.Core.Models;

namespace YarnShaper.Core.Tests.Algorithms;

public class SockHeelCalculatorTests
{
    private static readonly Gauge WorstedGauge = new(StitchesPerInch: 5.5, RowsPerInch: 7.5);

    [Fact]
    public void ProducesTheExactSchedulesForAKnownInput()
    {
        // 8in foot at 5.5 sts/in -> 44 sts, rounded to the nearest multiple of 8 -> 48 total, 24 heel sts.
        var rows = SockHeelCalculator.Calculate(WorstedGauge, new SockHeelMeasurements(FootCircumferenceInches: 8));

        var flap = rows.Where(r => r.Section == GarmentSection.HeelFlap).OrderBy(r => r.RowNumber).ToList();
        Assert.Equal(24, flap.Count);
        Assert.All(flap, r => Assert.Equal(24, r.StitchCount));
        Assert.All(flap, r => Assert.Equal(ShapingAction.None, r.Action));

        var turn = rows.Where(r => r.Section == GarmentSection.HeelTurn).OrderBy(r => r.RowNumber).ToList();
        Assert.Equal(10, turn.Count);
        Assert.Equal(Enumerable.Range(1, 10).Select(i => 24 - i), turn.Select(r => r.StitchCount));
        Assert.All(turn, r => Assert.Equal(ShapingAction.Decrease, r.Action));

        var gusset = rows.Where(r => r.Section == GarmentSection.Gusset).OrderBy(r => r.RowNumber).ToList();
        Assert.Equal(38, gusset[0].StitchCount);
        Assert.Equal(ShapingAction.Increase, gusset[0].Action);
        Assert.Equal(24, gusset[^1].StitchCount);
        Assert.Equal(ShapingAction.Decrease, gusset[^1].Action);
    }

    [Theory]
    [InlineData(6, 5.5, 7.5)]
    [InlineData(7.5, 6, 8)]
    [InlineData(9, 4.5, 6)]
    [InlineData(10.5, 7, 9)]
    public void HeelTurnEndsAtHalfHeelStitchesPlusTwo(double footInches, double stitchGauge, double rowGauge)
    {
        var gauge = new Gauge(stitchGauge, rowGauge);
        var rows = SockHeelCalculator.Calculate(gauge, new SockHeelMeasurements(footInches));

        var flapStitches = rows.First(r => r.Section == GarmentSection.HeelFlap).StitchCount;
        var lastTurnRow = rows.Where(r => r.Section == GarmentSection.HeelTurn).OrderBy(r => r.RowNumber).Last();

        Assert.Equal(flapStitches / 2 + 2, lastTurnRow.StitchCount);
    }

    [Theory]
    [InlineData(6, 5.5, 7.5)]
    [InlineData(7.5, 6, 8)]
    [InlineData(9, 4.5, 6)]
    [InlineData(10.5, 7, 9)]
    public void GussetDecreasesConvergeExactlyBackToHeelStitchCount(double footInches, double stitchGauge, double rowGauge)
    {
        var gauge = new Gauge(stitchGauge, rowGauge);
        var rows = SockHeelCalculator.Calculate(gauge, new SockHeelMeasurements(footInches));

        var heelStitches = rows.First(r => r.Section == GarmentSection.HeelFlap).StitchCount;
        var lastGussetRow = rows.Where(r => r.Section == GarmentSection.Gusset).OrderBy(r => r.RowNumber).Last();

        Assert.Equal(heelStitches, lastGussetRow.StitchCount);
    }

    [Fact]
    public void HeelFlapStitchCountIsConstant()
    {
        var rows = SockHeelCalculator.Calculate(WorstedGauge, new SockHeelMeasurements(8));

        var flap = rows.Where(r => r.Section == GarmentSection.HeelFlap).ToList();
        Assert.All(flap, r => Assert.Equal(flap[0].StitchCount, r.StitchCount));
        Assert.All(flap, r => Assert.Equal(ShapingAction.None, r.Action));
    }

    [Fact]
    public void HeelTurnStitchCountIsStrictlyDecreasingByOnePerRow()
    {
        var rows = SockHeelCalculator.Calculate(WorstedGauge, new SockHeelMeasurements(8));

        var turn = rows.Where(r => r.Section == GarmentSection.HeelTurn).OrderBy(r => r.RowNumber).ToList();
        for (var i = 1; i < turn.Count; i++)
        {
            Assert.Equal(turn[i - 1].StitchCount - 1, turn[i].StitchCount);
            Assert.Equal(ShapingAction.Decrease, turn[i].Action);
        }
    }

    [Fact]
    public void GussetStitchCountIsMonotonicNonIncreasingAfterPickup()
    {
        var rows = SockHeelCalculator.Calculate(WorstedGauge, new SockHeelMeasurements(8));

        var gusset = rows.Where(r => r.Section == GarmentSection.Gusset).OrderBy(r => r.RowNumber).ToList();
        for (var i = 1; i < gusset.Count; i++)
        {
            Assert.True(gusset[i].StitchCount <= gusset[i - 1].StitchCount);
        }
    }

    [Fact]
    public void ThrowsWhenFootCircumferenceIsTooSmallForAHeel()
    {
        var tinyGauge = new Gauge(StitchesPerInch: 1, RowsPerInch: 1);

        Assert.Throws<ArgumentException>(() =>
            SockHeelCalculator.Calculate(tinyGauge, new SockHeelMeasurements(FootCircumferenceInches: 1)));
    }
}
