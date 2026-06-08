# CI/CD setup — push-to-deploy via GitHub OIDC

Goal: every push to `main` builds + tests, packages the Lambda, and runs `terraform apply`
(PRs run `terraform plan`). Authentication uses GitHub OIDC — **no static AWS keys** in GitHub.

## One-time prerequisites

You need administrative AWS credentials locally for this one-time setup only.

```bash
aws configure          # access key / secret / region (e.g. us-east-1)
aws sts get-caller-identity   # confirm
```

### 1. Bootstrap AWS (S3 state bucket + OIDC deploy role)

```bash
AWS_REGION=us-east-1 ./scripts/bootstrap-cicd.sh
```

This creates:
- an **S3 bucket** for shared Terraform state (versioned, encrypted, private), and
- the **GitHub OIDC provider** + an IAM **deploy role** scoped to this repo (via `infra/bootstrap`).

It prints the three values you need next:
`AWS_REGION`, `TF_STATE_BUCKET`, `AWS_DEPLOY_ROLE_ARN`.

> If the account already has a GitHub OIDC provider, re-run with
> `terraform -chdir=infra/bootstrap apply -var create_oidc_provider=false ...` (the script's apply step),
> or import the existing provider.

### 2. Set the GitHub repo variables

```bash
gh variable set AWS_REGION          --body "us-east-1"
gh variable set TF_STATE_BUCKET     --body "geocode-cache-tfstate-<account-id>"
gh variable set AWS_DEPLOY_ROLE_ARN --body "arn:aws:iam::<account-id>:role/geocode-cache-github-deploy"
```

(`Settings → Secrets and variables → Actions → Variables` in the UI does the same.)
Until these exist, the `Deploy` workflow safely **skips** (it does not fail).

### 3. Provide the Google API key (one-time, out-of-band)

The first deploy creates an **empty** Secrets Manager secret. Set its value once — it never lives in
Terraform state or in CI:

```bash
aws secretsmanager put-secret-value \
  --secret-id geocode-cache/google-api-key \
  --secret-string "YOUR_GOOGLE_API_KEY"
```

(Do this after the first apply creates the secret, or pre-create the secret with the same name.)

## How it runs after that

- **Pull request → `main`**: `ci.yml` runs tests; `deploy.yml` runs `terraform plan` (no changes applied).
- **Push/merge → `main`**: `deploy.yml` builds, packages the Lambda, assumes the OIDC role, and runs
  `terraform apply` against the S3-backed state (native S3 locking, no DynamoDB lock table needed).

## Notes

- State locking uses the S3 backend's native lockfile (`use_lockfile=true`), so no separate lock table.
- The deploy role policy in `infra/bootstrap` is broad-by-service for the demo; tighten for production.
- Optional: configure a GitHub **Environment** named `dev` with required reviewers to gate applies.
- WAF (~$6/mo) and provisioned concurrency cost money; toggle via `enable_waf` / `provisioned_concurrency`
  in a `terraform.tfvars` (or via `TF_VAR_*` env in the workflow).
