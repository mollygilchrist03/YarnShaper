using YarnShaper.Core.Models;
using YarnShaper.Core.Yardage;

namespace YarnShaper.Core.Tests.Yardage;

public class YardageEstimatorTests
{
    private static readonly Gauge FiveStitchGauge = new(StitchesPerInch: 5, RowsPerInch: 7);

    [Fact]
    public void SplitsYardageByColorAndSumsWithinEachColor()
    {
        var rows = new (int StitchCount, string Color)[] { (10, "red"), (20, "red"), (36, "blue") };

        var result = YardageEstimator.EstimateYardageByColor(rows, FiveStitchGauge);

        // inchesPerStitch = 5.0 (multiplier) / 5 (sts/in) = 1.0
        Assert.Equal(30.0 / 36.0, result["red"], precision: 10);
        Assert.Equal(36.0 / 36.0, result["blue"], precision: 10);
    }

    [Fact]
    public void EmptyRowsProduceAnEmptyResult()
    {
        var result = YardageEstimator.EstimateYardageByColor(
            Array.Empty<(int, string)>(), FiveStitchGauge);

        Assert.Empty(result);
    }

    [Fact]
    public void FinerGaugeUsesLessYardagePerStitch()
    {
        var rows = new[] { (StitchCount: 100, Color: "red") };
        var coarseGauge = new Gauge(StitchesPerInch: 3, RowsPerInch: 5);
        var fineGauge = new Gauge(StitchesPerInch: 8, RowsPerInch: 10);

        var coarseYardage = YardageEstimator.EstimateYardageByColor(rows, coarseGauge)["red"];
        var fineYardage = YardageEstimator.EstimateYardageByColor(rows, fineGauge)["red"];

        Assert.True(fineYardage < coarseYardage);
    }

    [Fact]
    public void DoublingTheMultiplierDoublesTheYardage()
    {
        var rows = new[] { (StitchCount: 50, Color: "red") };

        var baseline = YardageEstimator.EstimateYardageByColor(rows, FiveStitchGauge, yarnPerStitchWidthMultiplier: 4.0)["red"];
        var doubled = YardageEstimator.EstimateYardageByColor(rows, FiveStitchGauge, yarnPerStitchWidthMultiplier: 8.0)["red"];

        Assert.Equal(baseline * 2, doubled, precision: 10);
    }

    [Fact]
    public void ThrowsWhenMultiplierIsNotPositive()
    {
        var rows = new[] { (StitchCount: 10, Color: "red") };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            YardageEstimator.EstimateYardageByColor(rows, FiveStitchGauge, yarnPerStitchWidthMultiplier: 0));
    }

    [Fact]
    public void ThrowsWhenRowsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            YardageEstimator.EstimateYardageByColor(null!, FiveStitchGauge));
    }

    [Fact]
    public void ThrowsWhenGaugeIsNull()
    {
        var rows = new[] { (StitchCount: 10, Color: "red") };

        Assert.Throws<ArgumentNullException>(() =>
            YardageEstimator.EstimateYardageByColor(rows, null!));
    }
}
