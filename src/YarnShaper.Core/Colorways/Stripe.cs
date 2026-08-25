namespace YarnShaper.Core.Colorways;

/// <summary>One block of a <see cref="StripeSequence"/>: a color held for a number of rows.</summary>
public sealed record Stripe
{
    public string Color { get; }
    public int RowCount { get; }

    public Stripe(string color, int rowCount)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Color must not be empty.", nameof(color));
        if (rowCount < 1)
            throw new ArgumentOutOfRangeException(nameof(rowCount), rowCount, "Row count must be at least 1.");

        Color = color;
        RowCount = rowCount;
    }
}
