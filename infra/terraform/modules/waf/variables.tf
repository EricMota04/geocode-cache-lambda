variable "name_prefix" {
  description = "Prefix for WAF resource names."
  type        = string
}

variable "stage_arn" {
  description = "ARN of the API Gateway stage to associate the Web ACL with."
  type        = string
}

variable "rate_limit" {
  description = "Max requests allowed from a single IP over a 5-minute window before blocking."
  type        = number
  default     = 2000
}

variable "tags" {
  description = "Tags to apply."
  type        = map(string)
  default     = {}
}
