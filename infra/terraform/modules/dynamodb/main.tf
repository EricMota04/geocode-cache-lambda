resource "aws_dynamodb_table" "cache" {
  name         = var.table_name
  billing_mode = "PAY_PER_REQUEST" # on-demand: scales automatically, no capacity planning
  hash_key     = "AddressKey"

  attribute {
    name = "AddressKey"
    type = "S"
  }

  # DynamoDB TTL handles background cleanup of expired rows. Correctness does not depend on it:
  # the application also checks ExpiresAt on read (deletion can lag up to ~48h).
  ttl {
    attribute_name = "ExpiresAt"
    enabled        = true
  }

  point_in_time_recovery {
    enabled = true
  }

  server_side_encryption {
    enabled     = true
    kms_key_arn = var.kms_key_arn
  }

  tags = var.tags
}
