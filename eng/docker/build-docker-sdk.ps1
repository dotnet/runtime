#!/usr/bin/env pwsh
# Builds libraries and produces a dotnet sdk docker image
# that contains the current bits in its shared framework folder.

[CmdletBinding(PositionalBinding=$false)]
Param(
  [string][Alias('t')]$imageName = "dotnet-sdk-libs-current",
  [string][Alias('c')]$configuration = "Release",
  [switch][Alias('w')]$buildWindowsContainers
)

$dotNetVersion="8.0"
$ErrorActionPreference = "Stop"

$REPO_ROOT_DIR=$(git -C "$PSScriptRoot" rev-parse --show-toplevel)

$dockerFilePrefix="$PSScriptRoot/libraries-sdk"

if ($buildWindowsContainers)
{
  # Due to size concerns, we don't currently do docker builds on windows.
  # Build on the host machine, then simply copy artifacts to the target docker image.
  # This should result in significantly lower build times, for now.
  & "$REPO_ROOT_DIR/build.cmd" clr+libs -ci -rc release -c $configuration

  if (!$?)
  {
    exit $LASTEXITCODE
  }

  $dockerFile="$dockerFilePrefix.windows.Dockerfile"
  
  # Collect the following artifacts to folder, that will be used as build context for the container,
  # so projects can build and test against the live-built runtime:
  # 1. Reference assembly pack (microsoft.netcore.app.ref)
  # 2. Runtime pack (microsoft.netcore.app.runtime.win-x64)
  # 3. targetingpacks.targets, so stress test builds can target the live-built runtime instead of the one in the pre-installed SDK
  # 4. testhost
  # 5. msquic interop sources (needed for HttpStress)
  $binArtifacts = "$REPO_ROOT_DIR\artifacts\bin"
  $dockerContext = "$REPO_ROOT_DIR\artifacts\docker-context"

  if (Test-Path $dockerContext) {
      Remove-Item -Recurse -Force $dockerContext
  }

  Copy-Item -Recurse -Path $binArtifacts\microsoft.netcore.app.ref `
                     -Destination $dockerContext\microsoft.netcore.app.ref
  Copy-Item -Recurse -Path $binArtifacts\microsoft.netcore.app.runtime.win-x64 `
                     -Destination $dockerContext\microsoft.netcore.app.runtime.win-x64
  Copy-Item -Recurse -Path $binArtifacts\testhost `
                     -Destination $dockerContext\testhost
  Copy-Item -Recurse -Path $REPO_ROOT_DIR\eng\targetingpacks.targets `
                     -Destination $dockerContext\targetingpacks.targets
  Copy-Item -Recurse -Path $REPO_ROOT_DIR\src\libraries\System.Net.Quic\src\System\Net\Quic\Interop `
                     -Destination $dockerContext\msquic-interop
  
  # In case of non-CI builds, testhost may already contain Microsoft.AspNetCore.App (see build-local.ps1 in HttpStress):
  $testHostAspNetCorePath="$dockerContext\testhost\net$dotNetVersion-windows-$configuration-x64/shared/Microsoft.AspNetCore.App"
  if (Test-Path $testHostAspNetCorePath) {
    Remove-Item -Recurse -Force $testHostAspNetCorePath
  }
  
  docker build --tag $imageName `
    --build-arg CONFIGURATION=$configuration `
    --file $dockerFile `
    $dockerContext
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
