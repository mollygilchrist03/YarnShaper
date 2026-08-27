using YarnShaper.Core.Models;

namespace YarnShaper.Core.Tests.AccuracyTests;

/// <summary>
/// Diffs a calculator's actual row schedule against a fixture's expected
/// rows and renders a per-row report, so a mismatch shows exactly which
/// rows diverge (and by how much) instead of a single pass/fail assertion.
/// </summary>
public static class RowScheduleComparer
{
    public static string? Diff(string sectionName, IReadOnlyList<ExpectedRow> expected, IReadOnlyList<ShapingRow> actual)
    {
        var actualByRow = actual.ToDictionary(r => r.RowNumber);
        var lines = new List<string>();

        foreach (var row in expected)
        {
            if (!actualByRow.TryGetValue(row.Row, out var actualRow))
            {
                lines.Add($"  row {row.Row}: expected {row.Stitches} sts ({row.Action}), actual: MISSING");
                continue;
            }

            if (actualRow.StitchCount != row.Stitches || actualRow.Action != row.Action)
            {
                lines.Add($"  row {row.Row}: expected {row.Stitches} sts ({row.Action}), " +
                          $"actual {actualRow.StitchCount} sts ({actualRow.Action})");
            }
        }

        var expectedRowNumbers = expected.Select(r => r.Row).ToHashSet();
        var extraRows = actual.Where(r => !expectedRowNumbers.Contains(r.RowNumber)).OrderBy(r => r.RowNumber);
        foreach (var extra in extraRows)
        {
            lines.Add($"  row {extra.RowNumber}: expected: MISSING, actual {extra.StitchCount} sts ({extra.Action})");
        }

        if (lines.Count == 0) return null;

        return $"{sectionName} diverges from the pattern on {lines.Count} row(s):{Environment.NewLine}" +
               string.Join(Environment.NewLine, lines);
    }
}
