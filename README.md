# Yarn Shaper

A Blazor WebAssembly app that computes garment shaping — raglan sweater
increases, sock heel turns, granny-square rounds — from body measurements
and gauge, then renders the resulting stitch/row geometry live as an SVG
schematic. A colorway layer sits on top so you can preview a stripe or
color sequence mapped onto the *actual* construction, not a generic
mockup.

Status: early scaffold — shaping calculators and the SVG renderer are in
progress. See `AGENTS.md` for the stack and solution layout.

## Running locally

```bash
dotnet build
dotnet test
dotnet run --project src/YarnShaper.Web
```

## Why this stack

Everything — including the shaping math — is C#, compiled to WebAssembly,
and runs entirely client-side. No backend, no auth, no database: just a
calculator and a renderer, deployed as a static site.
