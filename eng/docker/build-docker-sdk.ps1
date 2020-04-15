# Builds libraries and produces a dotnet sdk docker image
# that contains the current bits in its shared framework folder.

[CmdletBinding(PositionalBinding=$false)]
Param(
  [string][Alias('t')]$imageName = "dotnet-sdk-libs-current",
  [string][Alias('c')]$configuration = "Release",
  [switch][Alias('pa')]$privateAspNetCore
)

$ErrorActionPreference = "Stop"

$REPO_ROOT_DIR=$(git -C "$PSScriptRoot" rev-parse --show-toplevel)

# Due to size concerns, we don't currently do docker builds on windows.
# Build on the host machine, then simply copy artifacts to the target docker image.
# This should result in significantly lower build times, for now.
& "$REPO_ROOT_DIR/build.cmd" -ci -subset clr+libs -runtimeconfiguration release -c $configuration

if (!$?)
{
  exit $LASTEXITCODE
}

$dockerFile="$PSScriptRoot/libraries-sdk.windows.Dockerfile"

# Dockerize the build artifacts
if($privateAspNetCore)
{
  $dockerFile="$PSScriptRoot/libraries-sdk-aspnetcore.windows.Dockerfile"
}

docker build --tag $imageName `
  --build-arg CONFIGURATION=$configuration `
  --build-arg TESTHOST_LOCATION=. `
  --file $dockerFile `
  "$REPO_ROOT_DIR/artifacts/bin/testhost"

exit $LASTEXITCODE
