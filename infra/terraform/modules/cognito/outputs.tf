output "user_pool_arn" {
  description = "ARN of the Cognito user pool (for the API Gateway authorizer)."
  value       = aws_cognito_user_pool.this.arn
}

output "user_pool_id" {
  description = "Cognito user pool id."
  value       = aws_cognito_user_pool.this.id
}

output "client_id" {
  description = "Cognito app client id (used to obtain JWTs)."
  value       = aws_cognito_user_pool_client.this.id
}
