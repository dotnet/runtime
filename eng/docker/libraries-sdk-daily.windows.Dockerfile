# escape=`
# Simple Dockerfile which copies clr and library build artifacts into target dotnet sdk image
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:6.0-nanoserver-1809
FROM $SDK_BASE_IMAGE as target

SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

USER ContainerAdministrator

RUN Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile .\dotnet-install.ps1
RUN & .\dotnet-install.ps1 -Version 7.0.100-alpha.1.21518.14 -InstallDir 'C:\Program Files\dotnet'
RUN Get-ChildItem 'C:\Program Files\dotnet\shared\Microsoft.NETCore.App\7*' | Remove-Item -Recurse -Force

USER ContainerUser

ARG TESTHOST_LOCATION=".\\artifacts\\bin\\testhost"
ARG TFM=net7.0
ARG OS=windows
ARG ARCH=x64
ARG CONFIGURATION=Release

ARG COREFX_SHARED_FRAMEWORK_NAME=Microsoft.NETCore.App
ARG SOURCE_COREFX_VERSION=7.0.0
ARG TARGET_SHARED_FRAMEWORK="C:\\Program Files\\dotnet\\shared"
ARG TARGET_COREFX_VERSION=7.0.0

# COPY $TESTHOST_LOCATION C:/testhost

COPY `
    $TESTHOST_LOCATION\$TFM-$OS-$CONFIGURATION-$ARCH\shared\$COREFX_SHARED_FRAMEWORK_NAME\$SOURCE_COREFX_VERSION\ `
    $TARGET_SHARED_FRAMEWORK\$COREFX_SHARED_FRAMEWORK_NAME\$TARGET_COREFX_VERSION\

COPY $TESTHOST_LOCATION/DockerTest /app