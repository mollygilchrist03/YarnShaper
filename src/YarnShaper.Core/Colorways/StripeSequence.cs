namespace YarnShaper.Core.Colorways;

/// <summary>
/// A repeating color pattern — e.g. 4 rows navy, 2 rows cream — that
/// <see cref="ColorwayMapper"/> cycles across a shaping schedule's rows.
/// </summary>
public sealed record StripeSequence
{
    public IReadOnlyList<Stripe> Stripes { get; }

    /// <summary>Rows in one full repeat of the pattern.</summary>
    public int TotalRows { get; }

    public StripeSequence(IReadOnlyList<Stripe> stripes)
    {
        ArgumentNullException.ThrowIfNull(stripes);
        if (stripes.Count == 0)
            throw new ArgumentException("A stripe sequence needs at least one stripe.", nameof(stripes));

        Stripes = stripes;
        TotalRows = stripes.Sum(s => s.RowCount);
    }
}
