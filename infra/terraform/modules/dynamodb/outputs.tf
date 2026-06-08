output "table_name" {
  description = "The DynamoDB table name."
  value       = aws_dynamodb_table.cache.name
}

output "table_arn" {
  description = "The DynamoDB table ARN."
  value       = aws_dynamodb_table.cache.arn
}
