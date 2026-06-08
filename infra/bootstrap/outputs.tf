output "deploy_role_arn" {
  description = "ARN of the GitHub Actions deploy role. Set as the AWS_DEPLOY_ROLE_ARN repo variable."
  value       = aws_iam_role.deploy.arn
}
