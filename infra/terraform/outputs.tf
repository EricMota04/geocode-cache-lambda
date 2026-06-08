output "geocode_url" {
  description = "Full URL of the GET /Geocode endpoint."
  value       = module.apigateway.geocode_url
}

output "api_invoke_url" {
  description = "Base invoke URL for the deployed stage."
  value       = module.apigateway.invoke_url
}

output "dynamodb_table_name" {
  description = "Name of the DynamoDB cache table."
  value       = module.dynamodb.table_name
}

output "lambda_function_name" {
  description = "Name of the deployed Lambda function."
  value       = module.lambda.function_name
}

output "lambda_log_group" {
  description = "CloudWatch log group for the function."
  value       = module.lambda.log_group_name
}

output "google_api_key_secret_name" {
  description = "Populate this secret with the Google API key: aws secretsmanager put-secret-value --secret-id <name> --secret-string <KEY>"
  value       = aws_secretsmanager_secret.google_api_key.name
}

output "kms_key_arn" {
  description = "ARN of the application KMS key."
  value       = aws_kms_key.main.arn
}

output "cognito_user_pool_id" {
  description = "Cognito user pool id (for creating demo users and obtaining JWTs)."
  value       = module.cognito.user_pool_id
}

output "cognito_client_id" {
  description = "Cognito app client id (used with cognito-idp initiate-auth)."
  value       = module.cognito.client_id
}

output "waf_web_acl_arn" {
  description = "ARN of the WAF Web ACL (null when WAF is disabled)."
  value       = var.enable_waf ? module.waf[0].web_acl_arn : null
}
