resource "aws_api_gateway_rest_api" "this" {
  name = "${var.name_prefix}-api"

  endpoint_configuration {
    types = ["REGIONAL"]
  }

  tags = var.tags
}

locals {
  use_cognito = length(var.cognito_user_pool_arns) > 0
}

resource "aws_api_gateway_resource" "geocode" {
  rest_api_id = aws_api_gateway_rest_api.this.id
  parent_id   = aws_api_gateway_rest_api.this.root_resource_id
  path_part   = "Geocode"
}

resource "aws_api_gateway_authorizer" "cognito" {
  count           = local.use_cognito ? 1 : 0
  name            = "${var.name_prefix}-cognito"
  rest_api_id     = aws_api_gateway_rest_api.this.id
  type            = "COGNITO_USER_POOLS"
  provider_arns   = var.cognito_user_pool_arns
  identity_source = "method.request.header.Authorization"
}

resource "aws_api_gateway_method" "get" {
  rest_api_id   = aws_api_gateway_rest_api.this.id
  resource_id   = aws_api_gateway_resource.geocode.id
  http_method   = "GET"
  authorization = local.use_cognito ? "COGNITO_USER_POOLS" : "NONE"
  authorizer_id = local.use_cognito ? aws_api_gateway_authorizer.cognito[0].id : null

  request_parameters = {
    "method.request.querystring.address" = true
  }
}

resource "aws_api_gateway_integration" "lambda" {
  rest_api_id             = aws_api_gateway_rest_api.this.id
  resource_id             = aws_api_gateway_resource.geocode.id
  http_method             = aws_api_gateway_method.get.http_method
  type                    = "AWS_PROXY"
  integration_http_method = "POST" # Lambda proxy integrations are always invoked via POST
  uri                     = var.lambda_invoke_arn
}

resource "aws_lambda_permission" "apigw" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = var.lambda_function_name
  qualifier     = var.lambda_qualifier
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_api_gateway_rest_api.this.execution_arn}/*/*"
}

resource "aws_api_gateway_deployment" "this" {
  rest_api_id = aws_api_gateway_rest_api.this.id

  # Force a new deployment whenever the API surface changes.
  triggers = {
    redeployment = sha1(jsonencode([
      aws_api_gateway_resource.geocode.id,
      aws_api_gateway_method.get.id,
      aws_api_gateway_method.get.authorization,
      aws_api_gateway_method.get.authorizer_id,
      aws_api_gateway_integration.lambda.id,
      aws_api_gateway_integration.lambda.uri,
    ]))
  }

  lifecycle {
    create_before_destroy = true
  }

  depends_on = [aws_api_gateway_integration.lambda]
}

resource "aws_api_gateway_stage" "this" {
  rest_api_id          = aws_api_gateway_rest_api.this.id
  deployment_id        = aws_api_gateway_deployment.this.id
  stage_name           = var.stage_name
  xray_tracing_enabled = true
  tags                 = var.tags
}

resource "aws_api_gateway_method_settings" "this" {
  rest_api_id = aws_api_gateway_rest_api.this.id
  stage_name  = aws_api_gateway_stage.this.stage_name
  method_path = "*/*"

  settings {
    metrics_enabled = true
    # Execution logging (logging_level) requires an account-level CloudWatch role; left OFF
    # to keep the stack self-contained. Throttling protects the backend and Google quota.
    logging_level          = "OFF"
    throttling_rate_limit  = var.throttling_rate_limit
    throttling_burst_limit = var.throttling_burst_limit
  }
}
