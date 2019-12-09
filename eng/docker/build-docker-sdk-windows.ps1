# Builds libraries and produces a dotnet sdk docker image
# that contains the current bits in its shared framework folder.

[CmdletBinding(PositionalBinding=$false)]
Param(
  [string][Alias('t')]$imageName = "dotnet-sdk-libs-current",
  [string][Alias('c')]$configuration = "Release"
)

Set-Location $PSScriptRoot
$REPO_ROOT_DIR=$(git rev-parse --show-toplevel)

# Due to size concerns, we don't currently do docker builds on windows.
# Build on the host machine, then simply copy artifacts to the target docker image.
# This should result in significantly lower build times, for now.
& "$REPO_ROOT_DIR/libraries.cmd" -ci -c $configuration

# Dockerize the build artifacts
docker build --tag $imageName `
    --build-arg CONFIGURATION=$configuration `
    --build-arg TESTHOST_LOCATION=. `
    --file libraries-sdk.windows.Dockerfile `
    "$REPO_ROOT_DIR/artifacts/bin/testhost"
