data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

locals {
  name_prefix = "${var.project_name}-${var.environment}"

  common_tags = merge(
    {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "terraform"
    },
    var.tags,
  )
}

# ---- Customer-managed KMS key (encrypts the table, the secret, and the Lambda log group) ----

data "aws_iam_policy_document" "kms" {
  statement {
    sid    = "EnableRootAccount"
    effect = "Allow"
    principals {
      type        = "AWS"
      identifiers = ["arn:aws:iam::${data.aws_caller_identity.current.account_id}:root"]
    }
    actions   = ["kms:*"]
    resources = ["*"]
  }

  # CloudWatch Logs must be able to use the key to encrypt the Lambda log group.
  statement {
    sid    = "AllowCloudWatchLogs"
    effect = "Allow"
    principals {
      type        = "Service"
      identifiers = ["logs.${data.aws_region.current.name}.amazonaws.com"]
    }
    actions = [
      "kms:Encrypt",
      "kms:Decrypt",
      "kms:ReEncrypt*",
      "kms:GenerateDataKey*",
      "kms:Describe*",
    ]
    resources = ["*"]
    condition {
      test     = "ArnLike"
      variable = "kms:EncryptionContext:aws:logs:arn"
      values   = ["arn:aws:logs:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${local.name_prefix}-*"]
    }
  }
}

resource "aws_kms_key" "main" {
  description             = "${local.name_prefix} application encryption key"
  enable_key_rotation     = true
  deletion_window_in_days = 7
  policy                  = data.aws_iam_policy_document.kms.json
  tags                    = local.common_tags
}

resource "aws_kms_alias" "main" {
  name          = "alias/${local.name_prefix}"
  target_key_id = aws_kms_key.main.key_id
}

# ---- Secret container (value is set out-of-band; never stored in Terraform state) ----

resource "aws_secretsmanager_secret" "google_api_key" {
  name                    = var.google_api_key_secret_name
  description             = "Google Geocoding API key for ${local.name_prefix}"
  kms_key_id              = aws_kms_key.main.arn
  recovery_window_in_days = 7
  tags                    = local.common_tags
}

# ---- Application stack ----

module "dynamodb" {
  source      = "./modules/dynamodb"
  table_name  = "${local.name_prefix}-cache"
  kms_key_arn = aws_kms_key.main.arn
  tags        = local.common_tags
}

module "iam" {
  source      = "./modules/iam"
  name_prefix = local.name_prefix
  table_arn   = module.dynamodb.table_arn
  secret_arn  = aws_secretsmanager_secret.google_api_key.arn
  kms_key_arn = aws_kms_key.main.arn
  tags        = local.common_tags
}

module "cognito" {
  source      = "./modules/cognito"
  name_prefix = local.name_prefix
  tags        = local.common_tags
}

module "lambda" {
  source                  = "./modules/lambda"
  function_name           = "${local.name_prefix}-geocode"
  role_arn                = module.iam.role_arn
  package_path            = var.lambda_package_path
  handler                 = var.lambda_handler
  memory_mb               = var.lambda_memory_mb
  timeout_seconds         = var.lambda_timeout_seconds
  kms_key_arn             = aws_kms_key.main.arn
  log_retention_days      = var.log_retention_days
  provisioned_concurrency = var.provisioned_concurrency

  environment = {
    DynamoDb__TableName      = module.dynamodb.table_name
    Google__ApiKeySecretName = aws_secretsmanager_secret.google_api_key.name
    Google__BaseUrl          = var.google_base_url
    Cache__TtlDays           = tostring(var.cache_ttl_days)
    Cache__NegativeTtlDays   = tostring(var.negative_ttl_days)
    AWS__Region              = var.aws_region
  }

  tags = local.common_tags
}

module "apigateway" {
  source                 = "./modules/apigateway"
  name_prefix            = local.name_prefix
  lambda_invoke_arn      = module.lambda.invoke_arn
  lambda_function_name   = module.lambda.function_name
  lambda_qualifier       = module.lambda.qualifier
  cognito_user_pool_arns = var.enable_cognito_auth ? [module.cognito.user_pool_arn] : []
  throttling_rate_limit  = var.api_throttling_rate_limit
  throttling_burst_limit = var.api_throttling_burst_limit
  tags                   = local.common_tags
}

module "waf" {
  count       = var.enable_waf ? 1 : 0
  source      = "./modules/waf"
  name_prefix = local.name_prefix
  stage_arn   = module.apigateway.stage_arn
  rate_limit  = var.waf_rate_limit
  tags        = local.common_tags
}

module "monitoring" {
  source        = "./modules/monitoring"
  name_prefix   = local.name_prefix
  function_name = module.lambda.function_name
  api_name      = module.apigateway.api_name
  stage_name    = module.apigateway.stage_name
  region        = var.aws_region
  tags          = local.common_tags
}
