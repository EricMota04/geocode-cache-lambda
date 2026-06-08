# Geocode Cache — AWS Lambda + DynamoDB (.NET)

A production-minded AWS Lambda (C# / .NET) that proxies the **Google Geocoding API** for U.S.
addresses and caches the **full** Google response in **DynamoDB for 30 days**. The first request for
an address calls Google; subsequent requests are served from the cache until the TTL expires, after
which Google is called again.

```
GET /Geocode?address=70 Vanderbilt Ave, New York, NY 10017, United States
→ 200  { ...full Google Geocoding response... }   X-Cache: MISS   (first call)
→ 200  { ...full Google Geocoding response... }   X-Cache: HIT    (within 30 days)
```

> Status: built incrementally. See [Delivery stages](#delivery-stages) for what's landed so far.

## Why this design

The brief asks for the caching behavior *and* enterprise-grade design. This repo treats the second
part as a first-class goal:

| Concern | Approach |
| --- | --- |
| Architecture | Clean layering — `Domain` (pure) ← `Application` ← `Infrastructure` / `Lambda` (composition root). |
| Caching correctness | DynamoDB TTL for cleanup, **plus** an explicit `ExpiresAt > now` check on read so expiry is correct even if TTL deletion lags. |
| Negative caching | `ZERO_RESULTS` is cached with a **shorter** TTL (default 1 day) so typo'd addresses recover sooner; transient/config errors are **not** cached. |
| Secrets | Google API key in **AWS Secrets Manager** (cached at runtime); local dev falls back to an env var. Never hard-coded. |
| Config | Options pattern bound from `appsettings.json` + environment variables, validated on start. |
| Auth & edge | API Gateway (REST) + **Cognito** JWT authorizer + **AWS WAF** + stage throttling. |
| Observability | Structured CloudWatch logs, EMF custom metrics, X-Ray tracing. |
| IaC | **Terraform** stands up the entire stack (and deploys the Lambda) with one `apply`. |
| CI/CD | GitHub Actions: build + test on PR; package + `terraform plan`/`apply` via OIDC (no static keys). |
| Quality | Central Package Management, analyzers + warnings-as-errors, xUnit test suite that runs fully offline. |

## Repository layout

```
src/
  GeocodeCache.Domain          # models, abstractions (ports), cache-key logic — no AWS deps
  GeocodeCache.Application      # GeocodeService orchestration + caching policy + options
  GeocodeCache.Infrastructure  # DynamoDB, Google client, Secrets Manager adapters
  GeocodeCache.Lambda          # raw APIGatewayProxy handler; composition root
tests/
  GeocodeCache.UnitTests        # offline tests for the caching behavior + handler
  GeocodeCache.IntegrationTests # optional (DynamoDB Local), skipped in CI by default
infra/terraform                 # complete, deployable stack (DynamoDB, Lambda, API GW, Cognito, WAF, IAM)
.github/workflows               # CI (build/test) and deploy (package + terraform)
docs/                           # architecture notes + demo script
```

## Build & test (no AWS or Google account needed)

Requires the **.NET 10 SDK** (projects target **net8.0**, the Lambda `dotnet8` managed runtime).

```bash
dotnet build GeocodeCache.slnx -c Release
dotnet test  GeocodeCache.slnx -c Release --filter "Category!=Integration"
```

## Deploy

Documented as the Terraform and CI/CD stages land. High level:

```bash
# 1. store the Google API key (one time)
aws secretsmanager create-secret --name geocode-cache/google-api-key --secret-string "<KEY>"
# 2. stand up everything
cd infra/terraform/envs/dev && terraform init && terraform apply
```

## Delivery stages

- [x] **Stage 0** — Monorepo scaffold, solution, central build/package config, CI skeleton.
- [x] **Stage 1** — Domain + Application caching core + unit tests.
- [x] **Stage 2** — Infrastructure adapters (DynamoDB, Google, Secrets Manager).
- [ ] **Stage 3** — Lambda host (handler, DI, logging, metrics, tracing).
- [ ] **Stage 4** — Terraform core (DynamoDB, Lambda, REST API, IAM, secrets).
- [ ] **Stage 5** — Auth + edge (Cognito, WAF, throttling, provisioned concurrency).
- [ ] **Stage 6** — Full CI/CD (package + terraform plan/apply via OIDC).
- [ ] **Stage 7** — Hardening + docs (alarms/dashboard, stampede guard, architecture + demo).
