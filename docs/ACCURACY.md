# Accuracy against real patterns

Yarn Shaper's calculators are simplified, proportional models of each
construction — not a re-implementation of any specific published pattern's
exact technique. This document tracks how closely they line up with real
patterns, hand-traced row-by-row where the construction matches closely
enough for a comparison to mean anything, and documented as a known gap or
a non-applicable construction otherwise. Patterns are referenced only by
construction type, not by name or designer.

The automated half of this lives in
[`tests/YarnShaper.Core.Tests/AccuracyTests`](../tests/YarnShaper.Core.Tests/AccuracyTests) —
JSON fixtures with a pattern's gauge, measurements, and expected row-by-row
schedule, diffed against the calculator's actual output.

## Summary

| Pattern construction | Calculator | Result |
| --- | --- | --- |
| Heel-flap sock (flap + short-row turn + gusset) | `SockHeelShapingCalculator` | Heel flap and turn match exactly. Gusset has a documented 2-stitch / 1-round gap. |
| Top-down raglan with German short-row neck shaping | `RaglanShapingCalculator` | Not directly comparable — see below. |
| Shoulder-slope / set-in-sleeve tops (x2) | *(none)* | Not applicable — not a raglan construction. |

**Pass rate:** of the patterns whose construction actually matches an
implemented calculator, one section-for-section match (heel flap, heel
turn) and one documented, regression-pinned gap (gusset). The raglan
pattern's neckline technique isn't modeled at all yet, so it isn't counted
as a pass or fail — it's a scoping gap, tracked below.

## Heel-flap sock — `SockHeelShapingCalculator`

Traced at size Medium (foot circumference 9–10in, gauge 7 sts/in, CO 48
stitches, 24 heel stitches).

- **Heel flap** — the pattern works the flap for as many rows as there are
  heel stitches (24 rows on 24 stitches, matching `BuildHeelFlap`'s
  H-rows-for-H-stitches rule). Consistent across all three sizes given.
  **Matches exactly.**
- **Heel turn** — the pattern's short-row turn ends with 14 stitches
  remaining after 10 decrease rows for this size (and the equivalent ratio
  holds for the other two sizes given). `BuildHeelTurn`'s
  `heelStitches/2 - 2` row count and `heelStitches - rowNumber` stitch
  count formula reproduce this exactly. **Matches exactly.**
- **Gusset** — this is where the two diverge. The pattern's gusset pickup
  instructions pick up stitches along each side of the flap and *also* pick
  up one extra stitch "in the ladder in the row below" on each side, where
  the gusset meets the instep — a common technique for closing the small
  hole that otherwise forms there. That's 2 extra stitches total that get
  quietly decreased away in the first gusset-shaping round.
  `BuildGusset` doesn't model this: it picks up exactly
  `heelStitches / 2` per side and nothing else. The practical effect,
  worked out for size Medium:

  | Metric | Pattern | Calculator | Gap |
  | --- | --- | --- | --- |
  | Gusset peak stitches | 40 | 38 | −2 |
  | Decrease rounds | 8 | 7 | −1 |

  Final stitch count still lands correctly (both return to the original
  24 heel-side stitches), so the gap is invisible in the summary stats and
  only shows up if you're counting gusset rows against the pattern. This is
  pinned as a passing regression test
  (`GussetHasTheDocumentedLadderStitchGap`) rather than left as a silent
  divergence, so a future change to the algorithm either closes it on
  purpose or gets caught if it drifts further.

## Top-down raglan with short-row neck shaping — `RaglanShapingCalculator`

A real top-down raglan, but its neckline construction is different enough
from what `RaglanShapingCalculator` implements that a row-by-row comparison
wouldn't be meaningful yet:

- **German short-row neck shaping.** The pattern works several rounds of
  German short rows immediately after cast-on, increasing at each raglan
  line *during* the short rows to shape the back neck higher than the
  front. Cast-on stitches grow noticeably (72 → 88 stitches at the smallest
  size) before the "yoke" proper even starts. `RaglanShapingCalculator` has
  no short-row phase — its yoke increases start immediately from the
  cast-on count.
- **Explicit raglan-line stitches.** The pattern reserves a few stitches at
  each of the 4 raglan lines (12 stitches total, in this case) as a
  distinct category, separate from the back/front/sleeve counts — worked
  as a raglan detail rather than plain stockinette.
  `RaglanShapingCalculator` has no such category; 100% of the cast-on
  splits into back/front/sleeve/sleeve.
- **Different cast-on proportions.** With the raglan-line stitches set
  aside, the pattern's actual back/front/sleeve split at the smallest size
  (72 stitches) works out to roughly 37.5% / 37.5% / 4% / 4%.
  `RaglanShapingCalculator`'s fixed 30% / 30% / 20% / 20% split (see
  `BodyShare`/`SleeveShare` in the algorithm) produces a noticeably
  different shape for the same total, because this pattern is designed to
  be worn oversized and boxy rather than close-fitting through the
  shoulder.
- **Extra back-only rows.** After the yoke increases, the pattern knits a
  few additional rows across the back only (to raise the back neck) before
  splitting for sleeves — another phase `RaglanShapingCalculator` doesn't
  have.

None of this makes the calculator "wrong" — it's a different, simpler
raglan style (no short-row neck, symmetric front/back) that plenty of
patterns do use. But it means this specific construction can't validate
`RaglanShapingCalculator`'s math without first adding short-row neck
shaping and a raglan-line-stitch concept — which is a construction-coverage
change (see the roadmap's "Expand construction coverage" item), not an
accuracy bug. No fixture was added for this construction; revisit once that
support exists.

## Shoulder-slope tops — not applicable

Two patterns reviewed used a shoulder-slope / set-in-sleeve construction:
the shoulder is shaped with a straight, angled increase line at two
shoulder markers, not the four diagonal raglan lines a raglan yoke is built
around. There's currently no Yarn Shaper calculator for this construction,
so there's nothing to compare them against. Not counted as a failure —
just out of scope until a shoulder-slope calculator exists.

## Adding another fixture

1. Trace the pattern by hand for one size: work out the row-by-row stitch
   counts for whichever section(s) actually match an implemented
   calculator's construction.
2. Add a JSON file under `AccuracyTests/Fixtures/` with the pattern's
   gauge, measurements, and expected rows (see `sock-heel-medium-01.json`
   for the shape). Reference patterns by construction type only — no
   pattern names, designer names, or other identifying attribution in the
   fixture or in this document.
3. Add a test that loads it via `PatternFixture.Load(...)`, runs the
   relevant calculator, and diffs with `RowScheduleComparer.Diff(...)`.
4. If it doesn't match, don't force it green — pin the actual gap (like the
   gusset test above) and write up why here.
