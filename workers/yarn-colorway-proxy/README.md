# Yarn Colorway Proxy

A small Cloudflare Worker that proxies the [Yarn Colorways API](https://temperature-blanket.com/api)
(delivered via RapidAPI) so the Yarn Shaper web app — a static site with no server of its own — can
look up real yarn colorways near a hex color without shipping a RapidAPI secret key inside the public
WASM bundle.

It exposes a single route, `GET /match?color=<hex>`, and caches upstream responses for 24h using the
Workers Cache API so repeated lookups of the same/nearby colors don't burn through the free RapidAPI
tier's 500 calls/month.

This feature is **off by default** — the web app only calls this proxy if `YarnColorwayProxyUrl` is
set in `src/YarnShaper.Web/wwwroot/appsettings.json`. Skip this whole directory if you don't want the
yarn-color-matching feature.

## Setup

1. [Sign up for the Yarn Colorways API on RapidAPI](https://rapidapi.com/) and subscribe to a plan to
   get your `X-RapidAPI-Key`. The free plan allows 500 calls/month.
2. Install dependencies: `npm install`
3. Log in to Cloudflare: `npx wrangler login`
4. Store your RapidAPI key as a secret — **never commit it**: `npx wrangler secret put RAPIDAPI_KEY`
5. Deploy: `npm run deploy`
6. Wrangler prints the deployed URL, something like
   `https://yarn-colorway-proxy.<your-subdomain>.workers.dev`. Put that URL in
   `src/YarnShaper.Web/wwwroot/appsettings.json` as `YarnColorwayProxyUrl`, then rebuild/redeploy the
   web app.

## CORS

Allowed origins are hardcoded in `src/index.js` (`ALLOWED_ORIGINS`). Update that list if the site's
origin changes — a custom domain, a different GitHub username, etc.

## Local development

`npm run dev` runs the worker locally via `wrangler dev`. Provide your RapidAPI key through a local
`.dev.vars` file next to `wrangler.toml`:

```
RAPIDAPI_KEY=your-key-here
```

`.dev.vars` is gitignored — never commit it.
