# Yarn Shaper

A Blazor WebAssembly app that computes garment shaping (raglan sweater
increases, sock heel turns, granny-square rounds) from body measurements +
gauge, then renders the resulting stitch/row geometry live as an SVG
schematic — with a colorway layer on top so users can preview stripe/color
sequences mapped onto the actual construction, not a generic mockup.

## Stack

- Blazor WebAssembly, .NET 9 — entire app (including the shaping math) is
  C#, compiled to WASM, runs client-side. No Node, no JS framework.
- No backend for the MVP — pure client-side calculator + visualizer. No
  auth, no DB.
- Rendering: SVG generated directly from Blazor components/Razor markup —
  no Canvas/JS interop.
- Deploy target: GitHub Pages via GitHub Actions (static site).

## Solution structure

```
YarnShaper.sln
/src
  /YarnShaper.Core     <- class library, pure C#, no UI deps
      /Models          <- ShapingRow, GarmentSpec, Gauge, Measurement records
      /Algorithms      <- RaglanShapingCalculator, SockHeelCalculator, ...
      /Colorways        <- StripeSequence, ColorwayMapper
  /YarnShaper.Web      <- Blazor WebAssembly project (Pages/, Components/)
/tests
  /YarnShaper.Core.Tests  <- xUnit, tests the algorithms in isolation
```

## Local setup

```bash
dotnet build                          # build the whole solution
dotnet test                           # run YarnShaper.Core.Tests
dotnet run --project src/YarnShaper.Web   # serve the app locally
```

## Conventions

- `YarnShaper.Core` stays UI-agnostic and framework-free — it should read
  cleanly on its own, since the shaping algorithms are the point of the
  project. Model types are immutable records.
- Algorithm reasoning (e.g. how raglan increases are distributed evenly
  across rows) is documented via XML doc comments in the algorithm classes
  — that documentation is meant to double as README/portfolio material.
- `SchematicRenderer.razor` (Components/) is the single place that turns a
  `List<ShapingRow>` (+ optional colorway) into SVG — calculators and pages
  should produce row data and hand it to the renderer rather than drawing
  their own markup.
