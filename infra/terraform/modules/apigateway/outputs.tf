output "invoke_url" {
  description = "Base invoke URL for the deployed stage."
  value       = aws_api_gateway_stage.this.invoke_url
}

output "geocode_url" {
  description = "Full URL of the GET /Geocode endpoint."
  value       = "${aws_api_gateway_stage.this.invoke_url}/Geocode"
}

output "stage_arn" {
  description = "ARN of the stage (for WAF association)."
  value       = aws_api_gateway_stage.this.arn
}

output "rest_api_id" {
  description = "The REST API id."
  value       = aws_api_gateway_rest_api.this.id
}

output "api_name" {
  description = "The REST API name."
  value       = aws_api_gateway_rest_api.this.name
}

output "stage_name" {
  description = "The deployed stage name."
  value       = aws_api_gateway_stage.this.stage_name
}

output "execution_arn" {
  description = "The REST API execution ARN."
  value       = aws_api_gateway_rest_api.this.execution_arn
}
