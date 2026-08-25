namespace YarnShaper.Core.Colorways;

/// <summary>
/// Maps a <see cref="StripeSequence"/> onto a shaping schedule's row
/// numbers, cycling the pattern for as many rows as the schedule runs.
/// </summary>
/// <remarks>
/// A raglan yoke's four sections are worked in the round together, so row
/// N means the same physical round in every section — the mapping is keyed
/// by row number alone rather than per-section, and the same map applies
/// to every section's <see cref="Models.ShapingRow"/> list.
/// </remarks>
public static class ColorwayMapper
{
    public static IReadOnlyDictionary<int, string> MapRowColors(int totalRows, StripeSequence sequence)
    {
        if (totalRows < 1) throw new ArgumentOutOfRangeException(nameof(totalRows));
        ArgumentNullException.ThrowIfNull(sequence);

        var colors = new Dictionary<int, string>(totalRows);
        for (var rowNumber = 1; rowNumber <= totalRows; rowNumber++)
        {
            var positionInPattern = (rowNumber - 1) % sequence.TotalRows;
            colors[rowNumber] = ColorAtPosition(sequence, positionInPattern);
        }

        return colors;
    }

    private static string ColorAtPosition(StripeSequence sequence, int position)
    {
        var rowsSeen = 0;
        foreach (var stripe in sequence.Stripes)
        {
            rowsSeen += stripe.RowCount;
            if (position < rowsSeen) return stripe.Color;
        }

        throw new InvalidOperationException("Position must be within one repeat of the sequence.");
    }
}
