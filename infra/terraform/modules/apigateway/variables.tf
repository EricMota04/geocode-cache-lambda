variable "name_prefix" {
  description = "Prefix for API Gateway resource names."
  type        = string
}

variable "stage_name" {
  description = "API Gateway stage name."
  type        = string
  default     = "v1"
}

variable "lambda_invoke_arn" {
  description = "Invoke ARN of the backing Lambda function."
  type        = string
}

variable "lambda_function_name" {
  description = "Name of the backing Lambda function (for the invoke permission)."
  type        = string
}

variable "cognito_user_pool_arns" {
  description = "Cognito user pool ARNs. When non-empty, the GET method requires a Cognito JWT."
  type        = list(string)
  default     = []
}

variable "lambda_qualifier" {
  description = "Optional Lambda alias/version qualifier (set when provisioned concurrency is enabled)."
  type        = string
  default     = null
}

variable "throttling_rate_limit" {
  description = "Steady-state requests/second throttle for the stage."
  type        = number
  default     = 50
}

variable "throttling_burst_limit" {
  description = "Burst request throttle for the stage."
  type        = number
  default     = 100
}

variable "tags" {
  description = "Tags to apply."
  type        = map(string)
  default     = {}
}
