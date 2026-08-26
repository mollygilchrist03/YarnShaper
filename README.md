# Yarn Shaper

A garment-shaping calculator, not a form-and-database CRUD app: it turns
gauge and body measurements into the actual round-by-round stitch math a
hand-knit sweater is built from, then renders that math live as an SVG
schematic — no static mockup, no JS charting library, no backend. Everything,
including the shaping algorithms, is C# compiled to WebAssembly and run
entirely in the browser.

**Live demo:** [mollygilchrist03.github.io/YarnShaper](https://mollygilchrist03.github.io/YarnShaper/)

![Raglan yoke schematic — four sections rendered as stepped SVG trapezoids](docs/screenshots/raglan-calculator.png)

![Sock heel schematic — a constant-width flap, a narrowing short-row turn, and a gusset that picks up then decreases](docs/screenshots/sock-heel-calculator.png)

## What it does

Enter a gauge (stitches/rows per inch) and a set of finished measurements,
and the raglan calculator produces a full round-by-round shaping schedule
for a top-down raglan yoke — back, front, and both sleeves — then draws
each section as a schematic: cast-on at the neck, widening step by step
toward the underarm, exactly matching the computed stitch counts.

A colorway picker sits alongside it: build a stripe sequence (color + row
count per stripe), and it's mapped onto the real row data — all four
sections recolor in sync, since a raglan yoke is worked in the round and
row N is the same physical round in every section.

The sock heel calculator is a second, mechanically different
construction: a square heel flap, a short-row heel turn that narrows to a
point, and a gusset that picks up stitches along the flap and decreases
them back out — reusing the same schematic renderer and colorway picker,
proving the pipeline isn't raglan-specific.

## Notable engineering decisions

- **The shaping math is the actual point, so it's isolated and tested on
  its own.** `YarnShaper.Core` has zero UI dependencies — the raglan
  calculator ([`RaglanShapingCalculator.cs`](src/YarnShaper.Core/Algorithms/RaglanShapingCalculator.cs))
  is a pure function from gauge + measurements to a stitch schedule, with
  its reasoning written out as XML doc comments rather than left implicit
  in the code.
- **Distributing increases without clumping is a real algorithm, not a
  loop.** A raglan's four sections (back, front, two sleeves) each need a
  *different* number of increase rounds to reach their target
  circumference, but all share the same row budget. Naively front-loading
  N increases into the first N rows leaves a section idle for the rest of
  the piece. [`EvenDistribution.cs`](src/YarnShaper.Core/Algorithms/EvenDistribution.cs)
  spreads N events across M rows using the same integer error-accumulation
  technique as Bresenham's line algorithm — exact counts, no floating-point
  drift, no two events more than one row-gap apart.
- **Tests check invariants, not a memorized pattern.** Rather than
  hand-copying a "known good" schedule from a real pattern (and risking a
  silently-wrong fixture), the test suite asserts what has to be true of
  any correct schedule: monotonic stitch growth, exactly +2 stitches per
  increase round, convergence on the target circumference within rounding
  tolerance, and a hard failure when the yoke is too shallow for the
  required shaping. 48 tests, all in `YarnShaper.Core.Tests`.
- **The sock heel's stitch counts are rounded so the gusset math always
  divides evenly, not just for round numbers.** A heel turn always ends at
  H/2 + 2 stitches and a gusset always picks up 3H/2 + 2, so decreasing 2
  stitches per round only lands back on the original heel-needle count if
  that total is a multiple of 4. Rather than validating that constraint
  and rejecting awkward inputs,
  [`SockHeelShapingCalculator`](src/YarnShaper.Core/Algorithms/SockHeelShapingCalculator.cs)
  rounds the total round to the nearest multiple of 8 up front, so the
  convergence is exact for any gauge/circumference combination — the
  derivation is in the class's XML doc remarks alongside the code.
- **SVG straight from Razor, no Canvas/JS interop.** `SchematicRenderer.razor`
  takes a section's row list and draws it directly as stacked, proportionally
  widening `<rect>` bands — the "hard-to-fake" part of a schematic (increases
  visibly widening the shape) falls out of the data instead of being drawn
  by hand.
- **The colorway layer is keyed by absolute row number, not per-section
  index.** It would be simpler to map each section's own row list
  independently, but that's physically wrong for a raglan — back, front,
  and both sleeves are worked in the same round simultaneously.
  [`ColorwayMapper`](src/YarnShaper.Core/Colorways/ColorwayMapper.cs) maps
  the stripe pattern once against the shared row count and hands the same
  `RowNumber → color` map to every section, so a stripe boundary lines up
  identically across all four schematics.
- **CI gates the deploy on the test suite.** The GitHub Actions workflow
  runs `dotnet test` before it ever builds or publishes — a broken shaping
  calculation can't reach the live site.
- **GitHub Pages serves static files only, which breaks client-side
  routing.** A direct hit on `/raglan` 404s because there's no physical
  file at that path. The standard fix — copying `index.html` to `404.html`
  so Pages serves the app shell for any unmatched route while the browser
  keeps the real URL — is wired into the deploy workflow.

## Tech stack

| Layer | Choice |
|---|---|
| Framework | Blazor WebAssembly (.NET 9) |
| Shaping math | Plain C# class library (`YarnShaper.Core`), no UI dependencies |
| Rendering | Inline SVG generated from Razor components — no Canvas, no JS interop |
| Testing | xUnit (`YarnShaper.Core.Tests`) |
| CI/CD | GitHub Actions — test → publish → deploy |
| Hosting | GitHub Pages (static, free) |

## Local setup

```bash
dotnet build
dotnet test
dotnet run --project src/YarnShaper.Web
```

## What's next

Things planned but not yet built:

- Granny square / round-based calculator (round-by-round color is the
  classic use case for the colorway layer)
- Yardage estimator per colorway
- Export the schematic as a downloadable SVG/PNG
