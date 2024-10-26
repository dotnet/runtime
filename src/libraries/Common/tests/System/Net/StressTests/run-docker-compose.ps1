#!/usr/bin/env pwsh
# Runs the stress test using docker-compose

[CmdletBinding(PositionalBinding = $false)]
Param(
    [string]$TestProjectDir, # Path to the project directory
    [string][Alias('c')]$configuration = "Release", # Build configuration for libraries and stress suite
    [switch][Alias('w')]$useWindowsContainers, # Use windows containers, if available
    [switch][Alias('b')]$buildCurrentLibraries, # Drives the stress test using libraries built from current source
    [switch][Alias('pa')]$privateAspNetCore, # Drive the stress test using a private Asp.Net Core package, requires -b to be set
    [switch][Alias('o')]$buildOnly, # Build, but do not run the stress app
    [switch][Alias('n')]$noBuild, # Do not build the docker image
    [string][Alias('t')]$sdkImageName = "dotnet-sdk-libs-current", # Name of the sdk image name, if built from source.
    [string]$clientStressArgs = "",
    [string]$serverStressArgs = "",
    [string]$dumpsSharePath
)

$REPO_ROOT_DIR = $(git -C "$PSScriptRoot" rev-parse --show-toplevel)
$COMPOSE_FILE = "$TestProjectDir/docker-compose.yml"
[xml]$xml = Get-Content (Join-Path $RepoRoot "eng\Versions.props")
$VERSION = "$($xml.Project.PropertyGroup.MajorVersion[0]).$($xml.Project.PropertyGroup.MinorVersion[0])"

if (!$dumpsSharePath) {
    $dumpsSharePath = "$TestProjectDir/dumps"
}

# Build runtime libraries and place in a docker image
if ($buildCurrentLibraries) {
    $LIBRARIES_BUILD_ARGS = " -t $sdkImageName -c $configuration"
    if ($useWindowsContainers) {
        $LIBRARIES_BUILD_ARGS += " -w"
    }
    if ($privateAspNetCore) {
        $LIBRARIES_BUILD_ARGS += " -p"
    }

    Invoke-Expression "& $REPO_ROOT_DIR/eng/docker/build-docker-sdk.ps1 $LIBRARIES_BUILD_ARGS"

    if ($LASTEXITCODE -ne 0) { exit 1 }
}
elseif ($privateAspNetCore) {
    write-output "Using a private Asp.Net Core package (-pa) requires using privately built libraries. Please, enable it with -b switch."
    write-output "USAGE: . $($MyInvocation.InvocationName) -b -pa <args>"
    exit 1
}

if ($useWindowsContainers) {
    $env:DOCKERFILE = "windows.Dockerfile"
}

if (!$noBuild) {
    # Dockerize the stress app using docker-compose
    $BuildArgs = @(
        "--build-arg", "VERSION=$Version",
        "--build-arg", "CONFIGURATION=$configuration"
    )
    if ($sdkImageName) {
        $BuildArgs += "--build-arg", "SDK_BASE_IMAGE=$sdkImageName"
    }
    $originalErrorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        write-output "docker compose --file $COMPOSE_FILE build $buildArgs"
        docker compose --file $COMPOSE_FILE build @buildArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose exited with error code $LASTEXITCODE"
        }
    }
    finally {
        $ErrorActionPreference = $originalErrorPreference
    }
}

# Run the stress app
if (!$buildOnly) {
    if ($useWindowsContainers) {
        $env:DUMPS_SHARE_MOUNT_ROOT = "C:/dumps-share"
    }
    else {
        $env:DUMPS_SHARE_MOUNT_ROOT = "/dumps-share"
    }

    $env:DUMPS_SHARE = $dumpsSharePath
    New-Item -Force $env:DUMPS_SHARE -ItemType Directory

    $env:STRESS_CLIENT_ARGS = $clientStressArgs
    $env:STRESS_SERVER_ARGS = $serverStressArgs
    docker compose --file "$COMPOSE_FILE" up --abort-on-container-exit
}
