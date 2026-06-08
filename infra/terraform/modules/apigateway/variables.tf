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

variable "authorization" {
  description = "Method authorization type (NONE or COGNITO_USER_POOLS)."
  type        = string
  default     = "NONE"
}

variable "authorizer_id" {
  description = "API Gateway authorizer id (set when authorization is COGNITO_USER_POOLS)."
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
