#!/usr/bin/env bash
# One-time CI/CD bootstrap: creates the S3 Terraform-state bucket and the GitHub OIDC deploy role.
# Run locally with administrative AWS credentials. Prints the three values to set as GitHub repo variables.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REGION="${AWS_REGION:-us-east-1}"
ACCOUNT="$(aws sts get-caller-identity --query Account --output text)"
BUCKET="${TF_STATE_BUCKET:-geocode-cache-tfstate-${ACCOUNT}}"

echo "Account: ${ACCOUNT}   Region: ${REGION}   State bucket: ${BUCKET}"

# 1. State bucket (versioned, encrypted, private).
if aws s3api head-bucket --bucket "$BUCKET" 2>/dev/null; then
  echo "Bucket ${BUCKET} already exists."
else
  if [ "$REGION" = "us-east-1" ]; then
    aws s3api create-bucket --bucket "$BUCKET" --region "$REGION"
  else
    aws s3api create-bucket --bucket "$BUCKET" --region "$REGION" \
      --create-bucket-configuration LocationConstraint="$REGION"
  fi
  aws s3api put-bucket-versioning --bucket "$BUCKET" \
    --versioning-configuration Status=Enabled
  aws s3api put-bucket-encryption --bucket "$BUCKET" \
    --server-side-encryption-configuration '{"Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"AES256"}}]}'
  aws s3api put-public-access-block --bucket "$BUCKET" \
    --public-access-block-configuration BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true
  echo "Created bucket ${BUCKET}."
fi

# 2. GitHub OIDC provider + deploy role.
terraform -chdir="${ROOT}/infra/bootstrap" init -input=false
terraform -chdir="${ROOT}/infra/bootstrap" apply -auto-approve -input=false \
  -var "aws_region=${REGION}" \
  -var "state_bucket_arn=arn:aws:s3:::${BUCKET}"

ROLE_ARN="$(terraform -chdir="${ROOT}/infra/bootstrap" output -raw deploy_role_arn)"

echo
echo "=================================================================="
echo "Set these as GitHub repo variables (Settings > Variables > Actions):"
echo "  AWS_REGION          = ${REGION}"
echo "  TF_STATE_BUCKET     = ${BUCKET}"
echo "  AWS_DEPLOY_ROLE_ARN = ${ROLE_ARN}"
echo "=================================================================="
echo "Or with gh:"
echo "  gh variable set AWS_REGION --body '${REGION}'"
echo "  gh variable set TF_STATE_BUCKET --body '${BUCKET}'"
echo "  gh variable set AWS_DEPLOY_ROLE_ARN --body '${ROLE_ARN}'"
