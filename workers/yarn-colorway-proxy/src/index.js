// Proxies the Yarn Colorways API (RapidAPI) so the Yarn Shaper web app — a
// static site with no server of its own — can look up real yarn colorways
// near a hex color without shipping a RapidAPI secret key in the public
// WASM bundle. Responses are cached for 24h via the Workers Cache API to
// stay inside the free RapidAPI tier's 500 calls/month.

const ALLOWED_ORIGINS = new Set([
  "https://mollygilchrist03.github.io",
  "http://localhost:5075",
  "https://localhost:5075",
]);

const UPSTREAM_BASE = "https://yarn-colorways.p.rapidapi.com/v3";
const CACHE_TTL_SECONDS = 60 * 60 * 24;
const PASSTHROUGH_PARAMS = ["limit", "threshold", "brand", "yarn", "weight", "name", "exactName"];

function corsHeaders(origin) {
  const headers = {
    "Access-Control-Allow-Methods": "GET, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type",
    Vary: "Origin",
  };
  if (origin && ALLOWED_ORIGINS.has(origin)) {
    headers["Access-Control-Allow-Origin"] = origin;
  }
  return headers;
}

export default {
  async fetch(request, env, ctx) {
    const origin = request.headers.get("Origin");
    const cors = corsHeaders(origin);

    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: cors });
    }

    if (request.method !== "GET") {
      return new Response("Method not allowed", { status: 405, headers: cors });
    }

    const url = new URL(request.url);
    if (url.pathname !== "/match") {
      return new Response("Not found", { status: 404, headers: cors });
    }

    const color = url.searchParams.get("color");
    if (!color) {
      return new Response(JSON.stringify({ error: 'Missing required "color" query parameter.' }), {
        status: 400,
        headers: { ...cors, "Content-Type": "application/json" },
      });
    }

    const cache = caches.default;
    const cacheKey = new Request(url.toString(), request);
    const cached = await cache.match(cacheKey);
    if (cached) {
      const response = new Response(cached.body, cached);
      for (const [key, value] of Object.entries(cors)) response.headers.set(key, value);
      return response;
    }

    const upstream = new URL(`${UPSTREAM_BASE}/match/${encodeURIComponent(color)}`);
    for (const key of PASSTHROUGH_PARAMS) {
      const value = url.searchParams.get(key);
      if (value) upstream.searchParams.set(key, value);
    }

    const upstreamResponse = await fetch(upstream.toString(), {
      headers: {
        "X-RapidAPI-Key": env.RAPIDAPI_KEY,
        "X-RapidAPI-Host": "yarn-colorways.p.rapidapi.com",
      },
    });

    const body = await upstreamResponse.text();
    const response = new Response(body, {
      status: upstreamResponse.status,
      headers: {
        ...cors,
        "Content-Type": "application/json",
        "Cache-Control": `public, max-age=${CACHE_TTL_SECONDS}`,
      },
    });

    if (upstreamResponse.ok) {
      ctx.waitUntil(cache.put(cacheKey, response.clone()));
    }

    return response;
  },
};
