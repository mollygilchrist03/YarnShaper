using YarnShaper.Core.Models;

namespace YarnShaper.Core.Tests.Models;

public class GaugeTests
{
    [Theory]
    [InlineData(0, 7.5)]
    [InlineData(-5.5, 7.5)]
    [InlineData(5.5, 0)]
    [InlineData(5.5, -7.5)]
    public void ThrowsForNonPositiveStitchesOrRows(double stitchesPerInch, double rowsPerInch)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Gauge(stitchesPerInch, rowsPerInch));
    }

    [Fact]
    public void AcceptsPositiveStitchesAndRows()
    {
        var gauge = new Gauge(5.5, 7.5);

        Assert.Equal(5.5, gauge.StitchesPerInch);
        Assert.Equal(7.5, gauge.RowsPerInch);
    }
}
