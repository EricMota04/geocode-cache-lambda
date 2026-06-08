# API test matrix (Postman)

Import `docs/postman/GeocodeCache.postman_collection.json` and
`docs/postman/GeocodeCache.postman_environment.json` into Postman, select the environment, and fill in
`password` (the demo Cognito user's password). Run **Auth — Get JWT** first; it stores the token in the
`idToken` collection variable that every other request uses.

Endpoint: `GET {{baseUrl}}/Geocode?address=<US address>` with `Authorization: Bearer <JWT>`.

## Cases

| # | Scenario | Request | Auth | Expected | Why |
| --- | --- | --- | --- | --- | --- |
| 1 | **Get a JWT** | `POST cognito-idp.{{region}}.amazonaws.com` InitiateAuth | none | `200`, returns `IdToken` | Cognito issues the bearer token used below |
| 2 | **Success — cache MISS** | `GET /Geocode?address=70 Vanderbilt Ave, New York, NY 10017` | Bearer | `200`, body `status:"OK"`, header `X-Cache: MISS` | First lookup hits Google and caches it |
| 3 | **Success — cache HIT** | repeat case 2 | Bearer | `200`, `X-Cache: HIT` | Served from DynamoDB within TTL |
| 4 | **Normalization → HIT** | `GET /Geocode?address=  70   vanderbilt AVE, new york, ny 10017 ` | Bearer | `200`, `X-Cache: HIT` (after case 2) | Spacing/case normalize to the same cache key |
| 5 | **Negative cache (not found)** | `GET /Geocode?address=zzzz no such place 99999` | Bearer | `200`, body `status:"ZERO_RESULTS"` | Cached with the short negative TTL (1 day) |
| 6 | **Missing address** | `GET /Geocode` (no query) | Bearer | `400`, body `{"error":"missing_address"}` | Input validation |
| 7 | **Blank address** | `GET /Geocode?address=` | Bearer | `400`, `{"error":"missing_address"}` | Blank is rejected before any Google call |
| 8 | **No token** | `GET /Geocode?address=...` | none | `401 Unauthorized` | Cognito authorizer rejects unauthenticated calls |
| 9 | **Invalid/expired token** | `GET /Geocode?address=...` `Authorization: Bearer bad.token` | bad | `401 Unauthorized` | Authorizer validates the JWT |
| 10 | **Wrong method** | `POST /Geocode?address=...` | Bearer | `403` `Missing Authentication Token` | Only `GET` is defined on the resource |
| 11 | **Upstream config error** | `GET /Geocode?address=...` with an invalid Google key in the secret | Bearer | `200`, body `status:"REQUEST_DENIED"`, **not cached** | Google config errors are passed through and not cached |
| 12 | **Upstream transport failure → 502** | n/a (fault injection) | Bearer | `502`, `{"error":"upstream_error"}` | Network/non-2xx from Google maps to 502 (see note) |
| 13 | **Throttling** | burst > stage limit (Collection Runner, high concurrency) | Bearer | some `429 Too Many Requests` | API Gateway stage throttle (default 50 rps / 100 burst) |
| 14 | **WAF rate limit** | > rate limit from one IP in 5 min (Runner, many iterations) | Bearer | some `403` (WAF block) | WAF rate-based rule (default 2000 / 5 min / IP) |

### Notes
- **Cases 2–5** are the core caching demo: run case 2 once (MISS), then cases 3/4 show HIT, and case 5
  shows negative caching. Check `X-Cache` and the `ExpiresAt` attribute in DynamoDB.
- **Case 11** is easy to reproduce: temporarily put a bogus value in the secret
  (`aws secretsmanager put-secret-value --secret-id geocode-cache/google-api-key --secret-string bad`),
  call once (`REQUEST_DENIED`, not cached), then restore the real key.
- **Case 12 (502)** requires the provider to be unreachable/return non-2xx — not reproducible from
  Postman against the real Google API. It's covered by unit tests
  (`GoogleGeocodingClientTests`, `GeocodeRequestHandlerTests`). Document it as expected behavior.
- **Cases 13–14** need load (Postman **Collection Runner** with many iterations / parallel). The default
  limits are high, so lower `api_throttling_*` / `waf_rate_limit` in `terraform.tfvars` to demo them cheaply.
- A `401` returns Cognito/API-Gateway's own body (`{"message":"Unauthorized"}`); a `400/502` returns the
  app's envelope (`{"error","message"}`).
