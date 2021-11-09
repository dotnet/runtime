#!/usr/bin/env pwsh
# Builds libraries and produces a dotnet sdk docker image
# that contains the current bits in its shared framework folder.

[CmdletBinding(PositionalBinding=$false)]
Param(
  [string][Alias('t')]$imageName = "dotnet-sdk-libs-current",
  [string][Alias('c')]$configuration = "Release",
  [switch][Alias('w')]$buildWindowsContainers,
  [switch][Alias('ds')]$dailySdk
)

$ErrorActionPreference = "Stop"

$REPO_ROOT_DIR=$(git -C "$PSScriptRoot" rev-parse --show-toplevel)

$dockerFilePrefix="$PSScriptRoot/libraries-sdk"

if ($dailySdk)
{
  $dockerFilePrefix="$PSScriptRoot/libraries-sdk-daily"
}

if ($buildWindowsContainers)
{
  # Due to size concerns, we don't currently do docker builds on windows.
  # Build on the host machine, then simply copy artifacts to the target docker image.
  # This should result in significantly lower build times, for now.
  # & "$REPO_ROOT_DIR/build.cmd" clr+libs -ci -rc release -c $configuration

  # if (!$?)
  # {
  #   exit $LASTEXITCODE
  # }

  $dockerFile="$dockerFilePrefix-daily.windows.Dockerfile"
  
  ## Collect the following artifacts under to a special folder that and copy it to the container,
  ## so projects can build and test against the live-built runtime:
  ## 1. Reference assembly pack (microsoft.netcore.app.ref)
  ## 2. Runtime pack (microsoft.netcore.app.runtime.linux-x64)
  ## 3. targetingpacks.targets, so stress test builds can target the live-built runtime instead of the one in the pre-installed SDK
  ## 4. testhost
  $binArtifacts = "$REPO_ROOT_DIR\artifacts\bin"
  $dockerArtifacts = "$REPO_ROOT_DIR\artifacts\docker-context"

  if (Test-Path $dockerArtifacts) {
      Remove-Item -Recurse -Force $dockerArtifacts
  }

  Copy-Item -Recurse -Destination $dockerArtifacts\microsoft.netcore.app.ref -Path $binArtifacts\microsoft.netcore.app.ref
  Copy-Item -Recurse -Destination $dockerArtifacts\microsoft.netcore.app.runtime.win-x64 -Path $binArtifacts\microsoft.netcore.app.runtime.win-x64
  Copy-Item -Recurse -Destination $dockerArtifacts\testhost -Path $binArtifacts\testhost
  Copy-Item -Recurse -Destination $dockerArtifacts\targetingpacks.targets -Path $REPO_ROOT_DIR\eng\targetingpacks.targets
  
  docker build --tag $imageName `
    --build-arg CONFIGURATION=$configuration `
    --file $dockerFile `
    $dockerArtifacts
}
else
{
  # Docker build libraries and copy to dotnet sdk image
  $dockerFile="$dockerFilePrefix.linux.Dockerfile"

  docker build --tag $imageName `
      --build-arg CONFIGURATION=$configuration `
      --file $dockerFile `
      $REPO_ROOT_DIR
}

exit $LASTEXITCODE
