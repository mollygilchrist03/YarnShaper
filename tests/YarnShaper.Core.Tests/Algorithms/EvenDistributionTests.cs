using YarnShaper.Core.Algorithms;

namespace YarnShaper.Core.Tests.Algorithms;

public class EvenDistributionTests
{
    [Theory]
    [InlineData(10, 0)]
    [InlineData(10, 1)]
    [InlineData(10, 3)]
    [InlineData(10, 7)]
    [InlineData(10, 10)]
    [InlineData(17, 5)]
    [InlineData(1, 1)]
    [InlineData(1, 0)]
    public void PlacesExactlyItemCountItems(int totalSlots, int itemCount)
    {
        var result = EvenDistribution.Distribute(totalSlots, itemCount);

        Assert.Equal(totalSlots, result.Length);
        Assert.Equal(itemCount, result.Count(x => x));
    }

    [Theory]
    [InlineData(20, 3)]
    [InlineData(20, 7)]
    [InlineData(97, 11)]
    public void SpacesItemsWithinOneSlotOfIdealGap(int totalSlots, int itemCount)
    {
        var result = EvenDistribution.Distribute(totalSlots, itemCount);
        var positions = Enumerable.Range(0, totalSlots).Where(i => result[i]).ToArray();

        var idealGap = (double)totalSlots / itemCount;
        var gaps = positions.Zip(positions.Skip(1), (a, b) => b - a);

        Assert.All(gaps, gap => Assert.True(Math.Abs(gap - idealGap) < 1.0,
            $"gap {gap} deviates from ideal {idealGap:F2} by more than 1 slot"));
    }

    [Fact]
    public void ZeroItemsPlacesNothing()
    {
        var result = EvenDistribution.Distribute(10, 0);

        Assert.All(result, Assert.False);
    }

    [Fact]
    public void ItemCountEqualToSlotsFillsEverySlot()
    {
        var result = EvenDistribution.Distribute(5, 5);

        Assert.All(result, Assert.True);
    }

    [Fact]
    public void ThrowsWhenItemCountExceedsSlots()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EvenDistribution.Distribute(5, 6));
    }

    [Fact]
    public void ThrowsWhenSlotsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EvenDistribution.Distribute(-1, 0));
    }
}
