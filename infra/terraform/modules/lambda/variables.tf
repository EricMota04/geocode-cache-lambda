variable "function_name" {
  description = "Name of the Lambda function."
  type        = string
}

variable "role_arn" {
  description = "Execution role ARN."
  type        = string
}

variable "package_path" {
  description = "Path to the deployment zip."
  type        = string
}

variable "handler" {
  description = "Lambda handler string."
  type        = string
}

variable "memory_mb" {
  description = "Memory size in MB."
  type        = number
}

variable "timeout_seconds" {
  description = "Timeout in seconds."
  type        = number
}

variable "environment" {
  description = "Environment variables passed to the function."
  type        = map(string)
}

variable "kms_key_arn" {
  description = "KMS key ARN for encrypting the CloudWatch log group."
  type        = string
}

variable "log_retention_days" {
  description = "CloudWatch Logs retention in days."
  type        = number
}

variable "tags" {
  description = "Tags to apply."
  type        = map(string)
  default     = {}
}
