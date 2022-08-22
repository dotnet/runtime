# escape=`
# Simple Dockerfile which copies clr and library build artifacts into target dotnet sdk image
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:6.0
FROM $SDK_BASE_IMAGE as target

SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

ARG VERSION=7.0
ENV _DOTNET_INSTALL_CHANNEL="$VERSION.1xx"
ARG CONFIGURATION=Release

USER ContainerAdministrator

RUN Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile .\dotnet-install.ps1
RUN & .\dotnet-install.ps1 -Channel $env:_DOTNET_INSTALL_CHANNEL -Quality daily -InstallDir 'C:/Program Files/dotnet'

USER ContainerUser

COPY . /live-runtime-artifacts

# Add AspNetCore bits to testhost:
ENV _ASPNETCORE_SOURCE="C:/Program Files/dotnet/shared/Microsoft.AspNetCore.App/$VERSION*"
ENV _ASPNETCORE_DEST="C:/live-runtime-artifacts/testhost/net$VERSION-windows-$CONFIGURATION-x64/shared/Microsoft.AspNetCore.App"
RUN & New-Item -ItemType Directory -Path $env:_ASPNETCORE_DEST
RUN Copy-Item -Recurse -Path $env:_ASPNETCORE_SOURCE -Destination $env:_ASPNETCORE_DEST