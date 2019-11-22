# escape=`
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/core/sdk:3.0.100-nanoserver-1909
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 as corefxbuild

#### Build prerequisites
#### TODO: store in container registry for reproducible builds & reduced build times

# Install Visual Studio build tools
ADD https://aka.ms/vs/16/release/vs_buildtools.exe C:\TEMP\vs_buildtools.exe
ADD https://aka.ms/vs/16/release/channel C:\TEMP\VisualStudio.chman
ENV VS160COMNTOOLS="C:\\BuildTools\\Common7\\Tools"
ENV _VSCOMNTOOLS=$VS160COMNTOOLS
RUN cmd /S /C C:\TEMP\vs_buildtools.exe `
    --quiet --wait --norestart --nocache `
    --installPath C:\BuildTools `
    --channelUri C:\TEMP\VisualStudio.chman `
    --installChannelUri C:\TEMP\VisualStudio.chman `
    --add 'Microsoft.VisualStudio.Workload.VCTools;includeRecommended' `
    --add 'Microsoft.Component.MSBuild'

# Install scoop
RUN iwr -useb 'https://get.scoop.sh' | iex

# Install build dependencies
RUN scoop install cmake@3.15.5 python@3.8.0

#### Building the repo
WORKDIR C:\repo
COPY . .

ARG CONFIGURATION=Release
RUN .\libraries.cmd -c $env:CONFIGURATION

FROM $SDK_BASE_IMAGE as target

ARG TESTHOST_LOCATION="C:\\repo\\artifacts\\bin\\testhost"
ARG TFM=netcoreapp
ARG OS=Windows_NT
ARG ARCH=x64
ARG CONFIGURATION=Release

ARG COREFX_SHARED_FRAMEWORK_NAME=Microsoft.NETCore.App
ARG SOURCE_COREFX_VERSION=5.0.0
ARG TARGET_SHARED_FRAMEWORK="C:\\Program Files\\dotnet\\shared"
# NB version should be compatible with SDK_BASE_IMAGE tag
ARG TARGET_COREFX_VERSION=3.0.0

COPY --from=corefxbuild `
    $TESTHOST_LOCATION\$TFM-$OS-$CONFIGURATION-$ARCH\shared\$COREFX_SHARED_FRAMEWORK_NAME\$SOURCE_COREFX_VERSION\ `
    $TARGET_SHARED_FRAMEWORK\$COREFX_SHARED_FRAMEWORK_NAME\$TARGET_COREFX_VERSION\
