variable "table_name" {
  description = "Name of the DynamoDB cache table."
  type        = string
}

variable "kms_key_arn" {
  description = "KMS key ARN used for server-side encryption at rest."
  type        = string
}

variable "tags" {
  description = "Tags to apply."
  type        = map(string)
  default     = {}
}
