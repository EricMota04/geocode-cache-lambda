variable "name_prefix" {
  description = "Prefix for IAM resource names."
  type        = string
}

variable "table_arn" {
  description = "ARN of the DynamoDB cache table the function may access."
  type        = string
}

variable "secret_arn" {
  description = "ARN of the Secrets Manager secret holding the Google API key."
  type        = string
}

variable "kms_key_arn" {
  description = "ARN of the KMS key used to encrypt the table, logs and secret."
  type        = string
}

variable "tags" {
  description = "Tags to apply."
  type        = map(string)
  default     = {}
}
