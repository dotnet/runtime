#!/usr/bin/env pwsh
# Runs the stress test using docker-compose

[CmdletBinding(PositionalBinding=$false)]
Param(
    [string][Alias('c')]$configuration = "Release", # Build configuration for libraries and stress suite
    [switch][Alias('w')]$useWindowsContainers, # Use windows containers, if available
    [switch][Alias('b')]$buildCurrentLibraries, # Drives the stress test using libraries built from current source
    [switch][Alias('pa')]$privateAspNetCore, # Drive the stress test using a private Asp.Net Core package, requires -b to be set
    [switch][Alias('o')]$buildOnly, # Build, but do not run the stress app
    [string][Alias('t')]$sdkImageName, # Name of the sdk image name, if built from source.
    [string]$clientStressArgs = "",
    [string]$serverStressArgs = ""
)

$REPO_ROOT_DIR = $(git -C "$PSScriptRoot" rev-parse --show-toplevel)
$COMPOSE_FILE = "$PSScriptRoot/docker-compose.yml"

# Build runtime libraries and place in a docker image

if ($buildCurrentLibraries)
{
    if ([string]::IsNullOrEmpty($sdkImageName))
    {
        $sdkImageName = "dotnet-sdk-libs-current"
    }

    $LIBRARIES_BUILD_ARGS = " -t $sdkImageName -c $configuration"
    if($useWindowsContainers)
    {
        $LIBRARIES_BUILD_ARGS += " -w"
    }
    if($privateAspNetCore)
    {
        $LIBRARIES_BUILD_ARGS += " -p"
    }

    Invoke-Expression "& $REPO_ROOT_DIR/eng/docker/build-docker-sdk.ps1 $LIBRARIES_BUILD_ARGS"

    if (!$?) { exit 1 }
}
elseif ($privateAspNetCore) {
    write-output "Using a private Asp.Net Core package (-pa) requires using privately built libraries. Please, enable it with -b switch."
    write-output "USAGE: . $($MyInvocation.InvocationName) -b -pa <args>"
    exit 1
}

# Dockerize the stress app using docker-compose

$BUILD_ARGS = ""
if (![string]::IsNullOrEmpty($sdkImageName))
{
    $BUILD_ARGS += " --build-arg SDK_BASE_IMAGE=$sdkImageName"
}
if ($useWindowsContainers)
{
    $env:DOCKERFILE="windows.Dockerfile"
}

$originalErrorPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
	docker-compose --log-level DEBUG --file "$COMPOSE_FILE" build $BUILD_ARGS.Split() 2>&1 | ForEach-Object { "$_" }
	if ($LASTEXITCODE -ne 0) {
		throw "docker-compose exited with error code $LASTEXITCODE"
	}
}
finally {
	$ErrorActionPreference = $originalErrorPreference
}

# Run the stress app

if (!$buildOnly)
{
    if ($useWindowsContainers) {
        $env:DUMPS_SHARE_MOUNT_ROOT="C:/dumps-share"
    } else {
        $env:DUMPS_SHARE_MOUNT_ROOT="/dumps-share"
    }
    if (!$env:CLIENT_DUMPS_SHARE) {
        $env:CLIENT_DUMPS_SHARE=Join-Path $env:Temp $(New-Guid)
    }
    if (!$env:SERVER_DUMPS_SHARE) {
        $env:SERVER_DUMPS_SHARE=Join-Path $env:Temp $(New-Guid)
    }
    New-Item -Force $env:CLIENT_DUMPS_SHARE -ItemType Directory
    New-Item -Force $env:SERVER_DUMPS_SHARE -ItemType Directory

    $env:HTTPSTRESS_CLIENT_ARGS = $clientStressArgs
    $env:HTTPSTRESS_SERVER_ARGS = $serverStressArgs
    docker-compose --file "$COMPOSE_FILE" up --abort-on-container-exit
}
