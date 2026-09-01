// Proxies the Yarn Colorways API (RapidAPI) so the Yarn Shaper web app — a
// static site with no server of its own — can look up real yarn colorways
// by hex color or by a free-text yarn/brand search, without shipping a
// RapidAPI secret key in the public WASM bundle.

const ALLOWED_ORIGINS = new Set([
  "https://mollygilchrist03.github.io",
  "http://localhost:5075",
  "https://localhost:5075",
]);

const UPSTREAM_BASE = "https://yarn-colorways.p.rapidapi.com/v3";
const CACHE_TTL_SECONDS = 60 * 60 * 24;
const MAX_NAME_MATCHES = 20;

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

function jsonError(cors, status, message) {
  return new Response(JSON.stringify({ error: message }), {
    status,
    headers: { ...cors, "Content-Type": "application/json" },
  });
}

function upstreamHeaders(env) {
  return {
    headers: {
      "X-RapidAPI-Key": env.RAPIDAPI_KEY,
      "X-RapidAPI-Host": "yarn-colorways.p.rapidapi.com",
    },
  };
}

async function proxyToUpstream(request, env, ctx, url, upstreamPath, passthroughParams, cors) {
  const cache = caches.default;
  const cacheKey = new Request(url.toString(), request);
  const cached = await cache.match(cacheKey);
  if (cached) {
    const response = new Response(cached.body, cached);
    for (const [key, value] of Object.entries(cors)) response.headers.set(key, value);
    return response;
  }

  const upstream = new URL(`${UPSTREAM_BASE}${upstreamPath}`);
  for (const key of passthroughParams) {
    const value = url.searchParams.get(key);
    if (value) upstream.searchParams.set(key, value);
  }

  const upstreamResponse = await fetch(upstream.toString(), upstreamHeaders(env));
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
}

// /brands and /yarns list every brand/yarn name in the database — small,
// slow-changing payloads. Fetching (and caching) them lets us do our own
// substring matching, since the upstream `brand`/`yarn` filters only accept
// an exact name, not a partial one.
async function fetchListCached(cache, ctx, env, path) {
  const upstreamUrl = `${UPSTREAM_BASE}${path}`;
  const cacheKey = new Request(upstreamUrl);
  const cached = await cache.match(cacheKey);
  if (cached) return cached.json();

  const response = await fetch(upstreamUrl, upstreamHeaders(env));
  const body = await response.text();
  if (response.ok) {
    ctx.waitUntil(
      cache.put(
        cacheKey,
        new Response(body, {
          headers: { "Content-Type": "application/json", "Cache-Control": `public, max-age=${CACHE_TTL_SECONDS}` },
        }),
      ),
    );
  }
  return JSON.parse(body);
}

async function handleSearch(request, env, ctx, url, cors) {
  const q = (url.searchParams.get("q") || "").trim();
  if (!q) {
    return jsonError(cors, 400, 'Missing required "q" query parameter.');
  }
  const limit = Math.min(Number(url.searchParams.get("limit")) || 8, 50);

  const cache = caches.default;
  const cacheKey = new Request(url.toString(), request);
  const cached = await cache.match(cacheKey);
  if (cached) {
    const response = new Response(cached.body, cached);
    for (const [key, value] of Object.entries(cors)) response.headers.set(key, value);
    return response;
  }

  const lowerQ = q.toLowerCase();
  const yarns = await fetchListCached(cache, ctx, env, "/yarns");

  // Each /yarns entry already pairs a brand with a yarn line ("Cascade" +
  // "220 Superwash"), so matching the combined string handles both a
  // brand-only query ("malabrigo") and a brand+product query ("cascade
  // 220") — a query like "cascade 220" doesn't appear whole in either
  // field alone, only in their concatenation. Entries the API itself
  // flags `unavailable` (a fully discontinued yarn line) are dropped here
  // rather than fetched and filtered later — a single discontinued line
  // can have hundreds of colorway entries sorted first, which would
  // otherwise exhaust the fetch limit before reaching any live match.
  const matchingYarnNames = (yarns.data ?? [])
    .filter((y) => !y.unavailable && `${y.brandName ?? ""} ${y.yarnName ?? ""}`.toLowerCase().includes(lowerQ))
    .map((y) => y.yarnName)
    .filter((name, index, all) => name && all.indexOf(name) === index)
    .slice(0, MAX_NAME_MATCHES);

  // Yarn matches come first: someone typing "casc" almost certainly means
  // the Cascade brand, not a colorway that happens to be named "Cascade"
  // or "North Cascades". The colorway-name search runs last, as a
  // fallback for descriptive-color-name searches like "sage".
  //
  // Fetch a modest buffer beyond the display limit — individual colorways
  // (as opposed to whole discontinued yarn lines, already excluded above)
  // can still be flagged unavailable one at a time.
  const fetchLimit = Math.min(limit * 2, 50);
  const calls = [];
  if (matchingYarnNames.length > 0) {
    // Encode each name individually and join with a literal comma — the
    // upstream API's list params split on a raw ",", so encoding the
    // comma itself (as encodeURIComponent(a.join(",")) would) breaks
    // every multi-match query into one unmatchable garbled name.
    const yarnParam = matchingYarnNames.map(encodeURIComponent).join(",");
    calls.push(fetch(`${UPSTREAM_BASE}/colorways?yarn=${yarnParam}&limit=${fetchLimit}`, upstreamHeaders(env)));
  }
  calls.push(fetch(`${UPSTREAM_BASE}/colorways?name=${encodeURIComponent(q)}&limit=${fetchLimit}`, upstreamHeaders(env)));

  const responses = await Promise.all(calls);
  const bodies = await Promise.all(responses.map((r) => r.json().catch(() => ({ data: [] }))));

  const seen = new Set();
  const merged = [];
  outer: for (const body of bodies) {
    for (const item of body.data ?? []) {
      if (item.unavailable) continue;
      const key = `${item.brandId}/${item.yarnId}/${item.name}`;
      if (seen.has(key)) continue;
      seen.add(key);
      merged.push(item);
      if (merged.length >= limit) break outer;
    }
  }

  const responseBody = JSON.stringify({ meta: { limit, offset: 0, total: merged.length }, data: merged });
  const response = new Response(responseBody, {
    status: 200,
    headers: { ...cors, "Content-Type": "application/json", "Cache-Control": `public, max-age=${CACHE_TTL_SECONDS}` },
  });
  ctx.waitUntil(cache.put(cacheKey, response.clone()));
  return response;
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

    if (url.pathname === "/match") {
      const color = url.searchParams.get("color");
      if (!color) {
        return jsonError(cors, 400, 'Missing required "color" query parameter.');
      }
      const upstreamPath = `/match/${encodeURIComponent(color)}`;
      const passthroughParams = ["limit", "threshold", "brand", "yarn", "weight", "name", "exactName"];
      return proxyToUpstream(request, env, ctx, url, upstreamPath, passthroughParams, cors);
    }

    if (url.pathname === "/search") {
      return handleSearch(request, env, ctx, url, cors);
    }

    return new Response("Not found", { status: 404, headers: cors });
  },
};
