variable "aws_region" {
  description = "AWS region."
  type        = string
  default     = "us-east-1"
}

variable "github_owner" {
  description = "GitHub owner/org that hosts the repository."
  type        = string
  default     = "EricMota04"
}

variable "github_repo" {
  description = "GitHub repository name."
  type        = string
  default     = "geocode-cache-lambda"
}

variable "role_name" {
  description = "Name of the IAM role GitHub Actions assumes via OIDC."
  type        = string
  default     = "geocode-cache-github-deploy"
}

variable "create_oidc_provider" {
  description = "Create the GitHub OIDC provider. Set false if one already exists in the account."
  type        = bool
  default     = true
}

variable "state_bucket_arn" {
  description = "ARN of the S3 bucket holding Terraform state (for backend access). Use '*' to allow any."
  type        = string
  default     = "*"
}

variable "tags" {
  description = "Tags to apply."
  type        = map(string)
  default     = {}
}
