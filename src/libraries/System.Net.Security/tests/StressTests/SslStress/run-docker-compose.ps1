#!/usr/bin/env pwsh
# Runs the stress test using docker-compose

[CmdletBinding(PositionalBinding=$false)]
Param(
    [string][Alias('c')]$configuration = "Release", # Build configuration for libraries and stress suite
    [switch][Alias('w')]$useWindowsContainers, # Use windows containers, if available
    [switch][Alias('b')]$buildCurrentLibraries, # Drives the stress test using libraries built from current source
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
    $LIBRARIES_BUILD_ARGS = " -t $sdkImageName -c $configuration"
    if($useWindowsContainers)
    {
        $LIBRARIES_BUILD_ARGS += " -w"
    }

    Invoke-Expression "& $REPO_ROOT_DIR/eng/docker/build-docker-sdk.ps1 $LIBRARIES_BUILD_ARGS"

    if (!$?) { exit 1 }
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

docker-compose --file "$COMPOSE_FILE" build $BUILD_ARGS.Split()

# Run the stress app

if (!$buildOnly)
{
    $env:SSLSTRESS_CLIENT_ARGS = $clientStressArgs
    $env:SSLSTRESS_SERVER_ARGS = $serverStressArgs
    docker-compose --file "$COMPOSE_FILE" up --abort-on-container-exit
}
