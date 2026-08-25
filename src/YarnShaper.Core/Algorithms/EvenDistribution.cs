namespace YarnShaper.Core.Algorithms;

/// <summary>
/// Spreads a count of discrete events as evenly as possible across a fixed
/// number of slots — the classic "N increases over M rows" shaping problem.
/// </summary>
/// <remarks>
/// A naive approach front-loads all N increases into the first N slots,
/// which clumps them at the start of the piece instead of spreading the
/// shaping across its whole length. This uses the same integer
/// error-accumulation technique as Bresenham's line algorithm: walk the M
/// slots adding N to a running total each step, and place an event whenever
/// the total reaches or passes M (subtracting M so the remainder carries
/// forward). Because the accumulator only ever holds a remainder less than
/// M, no two events can be more than one slot closer together than any
/// other pair — the tightest possible spread — and the integer arithmetic
/// means exactly N events are placed with no floating-point drift.
/// </remarks>
public static class EvenDistribution
{
    /// <summary>
    /// Returns an array of <paramref name="totalSlots"/> booleans with
    /// exactly <paramref name="itemCount"/> of them true, spread as evenly
    /// as possible across the array.
    /// </summary>
    public static bool[] Distribute(int totalSlots, int itemCount)
    {
        if (totalSlots < 0) throw new ArgumentOutOfRangeException(nameof(totalSlots));
        if (itemCount < 0 || itemCount > totalSlots) throw new ArgumentOutOfRangeException(nameof(itemCount));

        var result = new bool[totalSlots];
        if (itemCount == 0 || totalSlots == 0) return result;

        var accumulator = 0;
        for (var slot = 0; slot < totalSlots; slot++)
        {
            accumulator += itemCount;
            if (accumulator < totalSlots) continue;

            accumulator -= totalSlots;
            result[slot] = true;
        }

        return result;
    }
}
