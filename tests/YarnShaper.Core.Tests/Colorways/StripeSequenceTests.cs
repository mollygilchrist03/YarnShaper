using YarnShaper.Core.Colorways;

namespace YarnShaper.Core.Tests.Colorways;

public class StripeSequenceTests
{
    [Fact]
    public void TotalRowsIsTheSumOfEachStripesRowCount()
    {
        var sequence = new StripeSequence([new Stripe("#ff0000", 4), new Stripe("#0000ff", 2)]);

        Assert.Equal(6, sequence.TotalRows);
    }

    [Fact]
    public void ThrowsWhenStripesIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new StripeSequence([]));
    }

    [Fact]
    public void ThrowsWhenStripesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new StripeSequence(null!));
    }

    [Fact]
    public void StripeThrowsOnEmptyColor()
    {
        Assert.Throws<ArgumentException>(() => new Stripe("", 2));
    }

    [Fact]
    public void StripeThrowsOnNonPositiveRowCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stripe("#ff0000", 0));
    }
}
