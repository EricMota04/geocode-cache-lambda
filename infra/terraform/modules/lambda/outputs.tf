output "function_name" {
  description = "The Lambda function name."
  value       = aws_lambda_function.this.function_name
}

output "function_arn" {
  description = "The Lambda function ARN."
  value       = aws_lambda_function.this.arn
}

output "invoke_arn" {
  description = "The Lambda invoke ARN (alias ARN when provisioned concurrency is enabled)."
  value       = local.pc_enabled ? aws_lambda_alias.live[0].invoke_arn : aws_lambda_function.this.invoke_arn
}

output "qualifier" {
  description = "Alias qualifier when provisioned concurrency is enabled, else null."
  value       = local.pc_enabled ? aws_lambda_alias.live[0].name : null
}

output "log_group_name" {
  description = "The CloudWatch log group name."
  value       = aws_cloudwatch_log_group.lambda.name
}
