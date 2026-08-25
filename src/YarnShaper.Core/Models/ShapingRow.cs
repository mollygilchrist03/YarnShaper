namespace YarnShaper.Core.Models;

public enum ShapingAction
{
    None,
    Increase,
    Decrease
}

/// <summary>
/// A named piece/section of a garment that shaping is tracked against
/// independently (each grows or shrinks on its own schedule).
/// </summary>
public enum GarmentSection
{
    Back,
    Front,
    LeftSleeve,
    RightSleeve,
    Round
}

/// <summary>
/// One row (or round) of a shaping schedule for a single <see cref="GarmentSection"/>:
/// how many stitches that section has, and whether this row shaped it.
/// A full schedule is a flat list of these, one entry per section per row.
/// </summary>
public sealed record ShapingRow(int RowNumber, int StitchCount, ShapingAction Action, GarmentSection Section);
