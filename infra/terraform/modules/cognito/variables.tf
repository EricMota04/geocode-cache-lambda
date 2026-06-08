variable "name_prefix" {
  description = "Prefix for Cognito resource names."
  type        = string
}

variable "tags" {
  description = "Tags to apply."
  type        = map(string)
  default     = {}
}
