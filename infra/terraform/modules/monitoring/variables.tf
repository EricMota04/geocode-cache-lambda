variable "name_prefix" {
  description = "Prefix for monitoring resource names."
  type        = string
}

variable "function_name" {
  description = "Lambda function name to monitor."
  type        = string
}

variable "api_name" {
  description = "API Gateway REST API name to monitor."
  type        = string
}

variable "stage_name" {
  description = "API Gateway stage name."
  type        = string
}

variable "region" {
  description = "AWS region (for dashboard widgets)."
  type        = string
}

variable "alarm_sns_topic_arn" {
  description = "Optional SNS topic ARN to notify on alarm. When null, alarms have no actions."
  type        = string
  default     = null
}

variable "tags" {
  description = "Tags to apply."
  type        = map(string)
  default     = {}
}
