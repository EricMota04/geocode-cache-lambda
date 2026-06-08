output "function_name" {
  description = "The Lambda function name."
  value       = aws_lambda_function.this.function_name
}

output "function_arn" {
  description = "The Lambda function ARN."
  value       = aws_lambda_function.this.arn
}

output "invoke_arn" {
  description = "The Lambda invoke ARN (for API Gateway integration)."
  value       = aws_lambda_function.this.invoke_arn
}

output "log_group_name" {
  description = "The CloudWatch log group name."
  value       = aws_cloudwatch_log_group.lambda.name
}
