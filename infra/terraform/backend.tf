# Example remote-state backend for shared/CI use. To enable:
#   1. Create an S3 bucket (and optionally a DynamoDB lock table).
#   2. Uncomment the `backend "s3" {}` line in versions.tf.
#   3. Run: terraform init -backend-config=backend.hcl
#
# backend.hcl:
#   bucket         = "my-tf-state-bucket"
#   key            = "geocode-cache/dev/terraform.tfstate"
#   region         = "us-east-1"
#   dynamodb_table = "my-tf-locks"
#   encrypt        = true
#
# Left as documentation so the project applies with local state out-of-the-box.
