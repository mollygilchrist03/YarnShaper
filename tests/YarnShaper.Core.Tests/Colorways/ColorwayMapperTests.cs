using YarnShaper.Core.Colorways;

namespace YarnShaper.Core.Tests.Colorways;

public class ColorwayMapperTests
{
    private static readonly StripeSequence TwoAndTwo =
        new([new Stripe("red", 2), new Stripe("blue", 2)]);

    [Fact]
    public void CyclesThePatternAcrossExactMultipleOfTotalRows()
    {
        var colors = ColorwayMapper.MapRowColors(8, TwoAndTwo);

        var expected = new[] { "red", "red", "blue", "blue", "red", "red", "blue", "blue" };
        for (var row = 1; row <= 8; row++)
        {
            Assert.Equal(expected[row - 1], colors[row]);
        }
    }

    [Fact]
    public void TruncatesThePatternWhenTotalRowsIsNotAnExactMultiple()
    {
        var colors = ColorwayMapper.MapRowColors(5, TwoAndTwo);

        var expected = new[] { "red", "red", "blue", "blue", "red" };
        for (var row = 1; row <= 5; row++)
        {
            Assert.Equal(expected[row - 1], colors[row]);
        }
    }

    [Fact]
    public void HandlesASequenceLongerThanTheRequestedRows()
    {
        var longSequence = new StripeSequence([new Stripe("red", 10)]);

        var colors = ColorwayMapper.MapRowColors(3, longSequence);

        Assert.Equal(3, colors.Count);
        Assert.All(colors.Values, c => Assert.Equal("red", c));
    }

    [Fact]
    public void ReturnsExactlyOneEntryPerRow()
    {
        var colors = ColorwayMapper.MapRowColors(37, TwoAndTwo);

        Assert.Equal(37, colors.Count);
        Assert.Equal(Enumerable.Range(1, 37), colors.Keys.OrderBy(k => k));
    }

    [Fact]
    public void ThrowsWhenTotalRowsIsLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ColorwayMapper.MapRowColors(0, TwoAndTwo));
    }

    [Fact]
    public void ThrowsWhenSequenceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ColorwayMapper.MapRowColors(4, null!));
    }
}
