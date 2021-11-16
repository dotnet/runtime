# escape=`
# Simple Dockerfile which copies clr and library build artifacts into target dotnet sdk image
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:6.0-nanoserver-1809
FROM $SDK_BASE_IMAGE as target

SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

USER ContainerAdministrator

RUN Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile .\dotnet-install.ps1
RUN & .\dotnet-install.ps1 -Channel 7.0.1xx -Quality daily -InstallDir 'C:/Program Files/dotnet'

USER ContainerUser

COPY . /live-runtime-artifacts

ARG VERSION=7.0
ARG CONFIGURATION=Release

ENV _ASPNETCORE_SOURCE_DIR="C:/Program Files/dotnet/shared/Microsoft.AspNetCore.App/$VERSION*"
ENV _ASPNETCORE_DEST_DIR="C:/live-runtime-artifacts/testhost/net$VERSION-windows-$CONFIGURATION-x64/shared/Microsoft.AspNetCore.App"
RUN & New-Item -ItemType Directory -Path $env:_ASPNETCORE_DEST_DIR
RUN Copy-Item -Recurse -Path $env:_ASPNETCORE_SOURCE_DIR -Destination $env:_ASPNETCORE_DEST_DIR

# COPY ./bin/microsoft.netcore.app.ref /live-runtime-artifacts/microsoft.netcore.app.ref
# COPY ./bin/microsoft.netcore.app.runtime.win-$ARCH /live-runtime-artifacts/microsoft.netcore.app.runtime.win-$ARCH
# COPY ./targetingpacks.targets /live-runtime-artifacts/targetingpacks.targets
# COPY ./bin/testhost /live-runtime-artifacts/testhost