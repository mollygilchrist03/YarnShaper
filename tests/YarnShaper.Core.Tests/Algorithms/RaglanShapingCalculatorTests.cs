using YarnShaper.Core.Algorithms;
using YarnShaper.Core.Models;

namespace YarnShaper.Core.Tests.Algorithms;

public class RaglanShapingCalculatorTests
{
    private static readonly Gauge DkGauge = new(StitchesPerInch: 5.5, RowsPerInch: 7.5);

    private static readonly RaglanMeasurements Adult = new(
        NeckCircumferenceInches: 20,
        BustCircumferenceInches: 40,
        UpperArmCircumferenceInches: 12,
        YokeDepthInches: 8);

    [Fact]
    public void ProducesAllFourSections()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult);
        var sections = rows.Select(r => r.Section).Distinct().OrderBy(s => s).ToArray();

        Assert.Equal(
            new[] { GarmentSection.Back, GarmentSection.Front, GarmentSection.LeftSleeve, GarmentSection.RightSleeve }
                .OrderBy(s => s),
            sections);
    }

    [Fact]
    public void FirstRowOfEachSectionHasNoActionAndCastOnCount()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult);
        var castOnStitches = DkGauge.StitchesFor(Adult.NeckCircumferenceInches);

        var firstRows = rows.Where(r => r.RowNumber == 1).ToArray();

        Assert.All(firstRows, r => Assert.Equal(ShapingAction.None, r.Action));
        Assert.Equal(castOnStitches, firstRows.Sum(r => r.StitchCount));
    }

    [Fact]
    public void StitchCountIsMonotonicNonDecreasingPerSection()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult);

        foreach (var section in rows.Select(r => r.Section).Distinct())
        {
            var ordered = rows.Where(r => r.Section == section).OrderBy(r => r.RowNumber).ToArray();
            for (var i = 1; i < ordered.Length; i++)
            {
                Assert.True(ordered[i].StitchCount >= ordered[i - 1].StitchCount);
            }
        }
    }

    [Fact]
    public void EveryIncreaseAddsExactlyTwoStitches()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult);

        foreach (var section in rows.Select(r => r.Section).Distinct())
        {
            var ordered = rows.Where(r => r.Section == section).OrderBy(r => r.RowNumber).ToArray();
            for (var i = 1; i < ordered.Length; i++)
            {
                var delta = ordered[i].StitchCount - ordered[i - 1].StitchCount;
                if (ordered[i].Action == ShapingAction.Increase)
                {
                    Assert.Equal(2, delta);
                }
                else
                {
                    Assert.Equal(0, delta);
                }
            }
        }
    }

    [Fact]
    public void BackAndFrontReachHalfBustCircumferenceWithinRoundingTolerance()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult);
        var target = DkGauge.StitchesFor(Adult.BustCircumferenceInches / 2);

        foreach (var section in new[] { GarmentSection.Back, GarmentSection.Front })
        {
            var finalCount = rows.Where(r => r.Section == section).Max(r => r.RowNumber) is var lastRow
                ? rows.First(r => r.Section == section && r.RowNumber == lastRow).StitchCount
                : 0;

            // Each increase round adds 2 sts, so the final count can overshoot
            // the target by at most 1 st (see IncreaseRoundsNeeded's ceiling).
            Assert.InRange(finalCount, target, target + 1);
        }
    }

    [Fact]
    public void SleevesReachUpperArmCircumferenceWithinRoundingTolerance()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult);
        var target = DkGauge.StitchesFor(Adult.UpperArmCircumferenceInches);

        foreach (var section in new[] { GarmentSection.LeftSleeve, GarmentSection.RightSleeve })
        {
            var lastRow = rows.Where(r => r.Section == section).Max(r => r.RowNumber);
            var finalCount = rows.First(r => r.Section == section && r.RowNumber == lastRow).StitchCount;

            Assert.InRange(finalCount, target, target + 1);
        }
    }

    [Fact]
    public void LeftAndRightSleevesAreIdenticalSchedules()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult);

        var left = rows.Where(r => r.Section == GarmentSection.LeftSleeve).OrderBy(r => r.RowNumber)
            .Select(r => (r.RowNumber, r.StitchCount, r.Action));
        var right = rows.Where(r => r.Section == GarmentSection.RightSleeve).OrderBy(r => r.RowNumber)
            .Select(r => (r.RowNumber, r.StitchCount, r.Action));

        Assert.Equal(left, right);
    }

    [Fact]
    public void AllSectionsSpanTheFullYokeRowCount()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult);
        var expectedRows = DkGauge.RowsFor(Adult.YokeDepthInches);

        foreach (var section in rows.Select(r => r.Section).Distinct())
        {
            var maxRow = rows.Where(r => r.Section == section).Max(r => r.RowNumber);
            Assert.Equal(expectedRows, maxRow);
        }
    }

    [Fact]
    public void ThrowsWhenYokeIsTooShallowForRequiredShaping()
    {
        var shallowYoke = Adult with { YokeDepthInches = 0.5 };

        Assert.Throws<ArgumentException>(() => RaglanShapingCalculator.Calculate(DkGauge, shallowYoke));
    }

    [Fact]
    public void PositiveEaseIncreasesTheFinalStitchCount()
    {
        var noEase = RaglanShapingCalculator.Calculate(DkGauge, Adult);
        var withEase = RaglanShapingCalculator.Calculate(DkGauge, Adult with { EaseInches = 6 });

        var noEaseBack = noEase.Where(r => r.Section == GarmentSection.Back).Max(r => r.StitchCount);
        var withEaseBack = withEase.Where(r => r.Section == GarmentSection.Back).Max(r => r.StitchCount);

        Assert.True(withEaseBack > noEaseBack);
    }

    [Fact]
    public void NegativeEaseDecreasesTheFinalStitchCount()
    {
        var noEase = RaglanShapingCalculator.Calculate(DkGauge, Adult);
        var withNegativeEase = RaglanShapingCalculator.Calculate(DkGauge, Adult with { EaseInches = -4 });

        var noEaseBack = noEase.Where(r => r.Section == GarmentSection.Back).Max(r => r.StitchCount);
        var negativeEaseBack = withNegativeEase.Where(r => r.Section == GarmentSection.Back).Max(r => r.StitchCount);

        Assert.True(negativeEaseBack < noEaseBack);
    }

    [Fact]
    public void EaseThatWouldMakeFinishedCircumferenceNonPositiveThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RaglanMeasurements(NeckCircumferenceInches: 20, BustCircumferenceInches: 40, UpperArmCircumferenceInches: 12, YokeDepthInches: 8, EaseInches: -40));
    }

    [Fact]
    public void BottomUpStartsAtUnderarmAndEndsNearNeckCastOnCount()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult, RaglanStyle.BottomUp);
        var castOnStitches = DkGauge.StitchesFor(Adult.NeckCircumferenceInches);

        var firstRows = rows.Where(r => r.RowNumber == 1).ToArray();
        Assert.All(firstRows, r => Assert.Equal(ShapingAction.None, r.Action));

        // Row 1 (the underarm join) should have far more stitches than the
        // final (neck) row.
        var back = rows.Where(r => r.Section == GarmentSection.Back).OrderBy(r => r.RowNumber).ToArray();
        Assert.True(back[0].StitchCount > back[^1].StitchCount);

        // Each section's final (neck) count should land within 1 stitch of
        // its share of the neck cast-on total top-down starts from — same
        // rounding tolerance as the top-down "reaches target" tests, since
        // decreasing to an odd target can undershoot by at most 1 stitch.
        var finalStitches = rows.GroupBy(r => r.Section).Sum(g => g.OrderBy(r => r.RowNumber).Last().StitchCount);
        Assert.InRange(finalStitches, castOnStitches - 4, castOnStitches);
    }

    [Fact]
    public void BottomUpStitchCountIsMonotonicNonIncreasingPerSection()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult, RaglanStyle.BottomUp);

        foreach (var section in rows.Select(r => r.Section).Distinct())
        {
            var ordered = rows.Where(r => r.Section == section).OrderBy(r => r.RowNumber).ToArray();
            for (var i = 1; i < ordered.Length; i++)
            {
                Assert.True(ordered[i].StitchCount <= ordered[i - 1].StitchCount);
            }
        }
    }

    [Fact]
    public void BottomUpEveryShapingRoundRemovesExactlyTwoStitches()
    {
        var rows = RaglanShapingCalculator.Calculate(DkGauge, Adult, RaglanStyle.BottomUp);

        foreach (var section in rows.Select(r => r.Section).Distinct())
        {
            var ordered = rows.Where(r => r.Section == section).OrderBy(r => r.RowNumber).ToArray();
            for (var i = 1; i < ordered.Length; i++)
            {
                var delta = ordered[i].StitchCount - ordered[i - 1].StitchCount;
                if (ordered[i].Action == ShapingAction.Decrease)
                {
                    Assert.Equal(-2, delta);
                }
                else
                {
                    Assert.Equal(0, delta);
                }
            }
        }
    }
}
