variable "aws_region" {
  description = "AWS region to deploy into."
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Project name used as a prefix for resource names."
  type        = string
  default     = "geocode-cache"
}

variable "environment" {
  description = "Deployment environment (e.g. dev, staging, prod)."
  type        = string
  default     = "dev"
}

variable "tags" {
  description = "Additional tags applied to all resources."
  type        = map(string)
  default     = {}
}

# ---- Lambda ----

variable "lambda_package_path" {
  description = "Path to the built Lambda deployment zip (produced by `dotnet lambda package`)."
  type        = string
  default     = "../../artifacts/GeocodeCache.Lambda.zip"
}

variable "lambda_handler" {
  description = "Lambda handler string (Assembly::Type::Method)."
  type        = string
  default     = "GeocodeCache.Lambda::GeocodeCache.Lambda.GeocodeFunction::FunctionHandlerAsync"
}

variable "lambda_memory_mb" {
  description = "Lambda memory size in MB."
  type        = number
  default     = 512
}

variable "lambda_timeout_seconds" {
  description = "Lambda timeout in seconds."
  type        = number
  default     = 30
}

# ---- Application config (passed to the Lambda as environment variables) ----

variable "google_api_key_secret_name" {
  description = "Name of the Secrets Manager secret holding the Google API key. Created (empty) by Terraform; set its value out-of-band."
  type        = string
  default     = "geocode-cache/google-api-key"
}

variable "google_base_url" {
  description = "Google Geocoding API base URL."
  type        = string
  default     = "https://maps.googleapis.com/maps/api/geocode/json"
}

variable "cache_ttl_days" {
  description = "TTL (days) for successful (OK) geocoding responses."
  type        = number
  default     = 30
}

variable "negative_ttl_days" {
  description = "TTL (days) for negative (ZERO_RESULTS) geocoding responses."
  type        = number
  default     = 1
}

# ---- Observability ----

variable "log_retention_days" {
  description = "CloudWatch Logs retention in days."
  type        = number
  default     = 14
}
