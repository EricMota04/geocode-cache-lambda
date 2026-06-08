locals {
  pc_enabled = var.provisioned_concurrency > 0
}

# Create the log group explicitly so retention + encryption are managed (not auto-created untagged).
resource "aws_cloudwatch_log_group" "lambda" {
  name              = "/aws/lambda/${var.function_name}"
  retention_in_days = var.log_retention_days
  kms_key_id        = var.kms_key_arn
  tags              = var.tags
}

resource "aws_lambda_function" "this" {
  function_name = var.function_name
  role          = var.role_arn
  runtime       = "dotnet8"
  handler       = var.handler
  architectures = ["x86_64"]

  filename         = var.package_path
  source_code_hash = filebase64sha256(var.package_path)

  # Publish an immutable version only when provisioned concurrency targets an alias.
  publish = local.pc_enabled

  memory_size = var.memory_mb
  timeout     = var.timeout_seconds

  environment {
    variables = var.environment
  }

  # Active tracing emits an X-Ray trace per invocation (SDK subsegments added in code).
  tracing_config {
    mode = "Active"
  }

  depends_on = [aws_cloudwatch_log_group.lambda]
  tags       = var.tags
}

# Optional warm capacity via an alias (only created when provisioned concurrency is enabled).
resource "aws_lambda_alias" "live" {
  count            = local.pc_enabled ? 1 : 0
  name             = "live"
  function_name    = aws_lambda_function.this.function_name
  function_version = aws_lambda_function.this.version
}

resource "aws_lambda_provisioned_concurrency_config" "this" {
  count                             = local.pc_enabled ? 1 : 0
  function_name                     = aws_lambda_function.this.function_name
  qualifier                         = aws_lambda_alias.live[0].name
  provisioned_concurrent_executions = var.provisioned_concurrency
}
