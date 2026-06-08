# Demo script (5–10 minutes)

A suggested narration + commands for recording the walkthrough. Assumes `terraform apply` has run and
the Google API key has been stored in the secret.

## 0. Setup (before recording)

```bash
# Deploy everything (packages the Lambda, then terraform apply)
make tf-apply

# Store the Google API key in the secret Terraform created
aws secretsmanager put-secret-value \
  --secret-id "$(terraform -chdir=infra/terraform output -raw google_api_key_secret_name)" \
  --secret-string "YOUR_GOOGLE_API_KEY"

# Capture outputs
cd infra/terraform
API=$(terraform output -raw geocode_url)
POOL=$(terraform output -raw cognito_user_pool_id)
CLIENT=$(terraform output -raw cognito_client_id)
TABLE=$(terraform output -raw dynamodb_table_name)
LOGS=$(terraform output -raw lambda_log_group)
cd ../..
```

## 1. Code tour (~2 min)

- **Clean layering** — `src/`: `Domain` (pure) ← `Application` ← `Infrastructure`/`Lambda`. Show
  `GeocodeService.GetGeocodeAsync` — the read-through cache + TTL/negative-cache policy in one place.
- **Ports & adapters** — `Domain/Abstractions` interfaces, implemented in `Infrastructure`.
- **Production touches** — Central Package Management, analyzers + warnings-as-errors, `LoggerMessage`
  source-gen logging, options validated on start, secrets never hard-coded.
- **Tests** — `dotnet test` → all unit tests green, fully offline (no AWS/Google needed).

```bash
dotnet test GeocodeCache.slnx -c Release --filter "Category!=Integration"
```

## 2. Auth: mint a Cognito JWT (~1 min)

```bash
aws cognito-idp admin-create-user --user-pool-id "$POOL" \
  --username demo@example.com --message-action SUPPRESS
aws cognito-idp admin-set-user-password --user-pool-id "$POOL" \
  --username demo@example.com --password 'Passw0rd!23' --permanent

JWT=$(aws cognito-idp initiate-auth --auth-flow USER_PASSWORD_AUTH \
  --client-id "$CLIENT" \
  --auth-parameters USERNAME=demo@example.com,PASSWORD='Passw0rd!23' \
  --query 'AuthenticationResult.IdToken' --output text)
```

Show that **without** a token the API rejects the request:

```bash
curl -i "$API?address=70 Vanderbilt Ave, New York, NY 10017"   # 401 Unauthorized
```

## 3. Cache MISS → HIT (~2 min)

```bash
# First call: cache miss -> Google is called, response cached. Note X-Cache: MISS
curl -sD - -H "Authorization: Bearer $JWT" \
  "$API?address=70 Vanderbilt Ave, New York, NY 10017, United States" | sed -n '1,20p'

# Second identical call: served from DynamoDB. Note X-Cache: HIT (and it's faster)
curl -sD - -H "Authorization: Bearer $JWT" \
  "$API?address=70 Vanderbilt Ave, New York, NY 10017, United States" | grep -i x-cache
```

Show the cached item + its TTL in DynamoDB:

```bash
aws dynamodb scan --table-name "$TABLE" \
  --projection-expression "AddressKey, GoogleStatus, ExpiresAt, IsNegative" --max-items 5
```

Point out `ExpiresAt` ≈ now + 30 days (epoch seconds).

## 4. Negative caching (~1 min)

```bash
curl -s -H "Authorization: Bearer $JWT" \
  "$API?address=asdfqwer no such place zzzz" | head -c 200
```

Returns the full Google body with `status: ZERO_RESULTS` (HTTP 200), cached with the **short** 1-day
TTL so a corrected address recovers quickly. Show `IsNegative=true` in the scan.

## 5. Observability (~2 min)

- **CloudWatch Logs** — structured hit/miss lines (no API key present):
  ```bash
  aws logs tail "$LOGS" --since 10m --format short
  ```
- **Metrics / dashboard** — open the `geocode-cache-dev` CloudWatch dashboard: cache HIT vs MISS,
  Lambda duration p95, API latency.
- **X-Ray** — open the service map for a trace (API Gateway → Lambda → DynamoDB/Secrets).
- **WAF** — show the Web ACL and its rule metrics (rate limit + managed rules).

## 6. Wrap-up (~1 min)

- One `terraform apply` provisions the whole stack; CI/CD (`deploy.yml`) does it via GitHub OIDC.
- Recap enterprise decisions: clean architecture, code-checked expiry (not just TTL), negative caching,
  Secrets Manager, least-privilege IAM + KMS, Cognito + WAF + throttling, structured logs/metrics/traces,
  and an offline-testable core. See `docs/architecture.md` for the hardening roadmap.
