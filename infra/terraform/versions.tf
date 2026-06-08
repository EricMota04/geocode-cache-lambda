terraform {
  required_version = ">= 1.6"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # Remote state backend. Left commented so the project applies out-of-the-box with local state;
  # uncomment and configure for shared/CI use (see backend.tf for a ready-to-fill example).
  # backend "s3" {}
}
