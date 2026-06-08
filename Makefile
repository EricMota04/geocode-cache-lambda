.PHONY: build test package fmt tf-init tf-validate tf-plan tf-apply tf-destroy

SOLUTION := GeocodeCache.slnx
TF_DIR := infra/terraform

build:
	dotnet build $(SOLUTION) -c Release

test:
	dotnet test $(SOLUTION) -c Release --filter "Category!=Integration"

package:
	./scripts/package-lambda.sh

fmt:
	dotnet format $(SOLUTION)
	terraform -chdir=$(TF_DIR) fmt -recursive

tf-init:
	terraform -chdir=$(TF_DIR) init

tf-validate:
	terraform -chdir=$(TF_DIR) validate

# Package the Lambda, then plan/apply the full stack.
tf-plan: package
	terraform -chdir=$(TF_DIR) plan

tf-apply: package
	terraform -chdir=$(TF_DIR) apply

tf-destroy:
	terraform -chdir=$(TF_DIR) destroy
