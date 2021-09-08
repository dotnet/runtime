# Builds and copies library artifacts into target dotnet sdk image
ARG BUILD_BASE_IMAGE=mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-f39df28-20191023143754
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:6.0-bullseye-slim

FROM $BUILD_BASE_IMAGE as corefxbuild

WORKDIR /repo
COPY . .

ARG CONFIGURATION=Release
RUN ./build.sh -ci -subset clr+libs -runtimeconfiguration release -c $CONFIGURATION

FROM $SDK_BASE_IMAGE as target

ARG TESTHOST_LOCATION=/repo/artifacts/bin/testhost
ARG TFM=net6.0
ARG OS=Linux
ARG ARCH=x64
ARG CONFIGURATION=Release

ARG COREFX_SHARED_FRAMEWORK_NAME=Microsoft.NETCore.App
ARG SOURCE_COREFX_VERSION=7.0.0
ARG TARGET_SHARED_FRAMEWORK=/usr/share/dotnet/shared
ARG TARGET_COREFX_VERSION=$DOTNET_VERSION

COPY --from=corefxbuild \
    $TESTHOST_LOCATION/$TFM-$OS-$CONFIGURATION-$ARCH/shared/$COREFX_SHARED_FRAMEWORK_NAME/$SOURCE_COREFX_VERSION/* \
    $TARGET_SHARED_FRAMEWORK/$COREFX_SHARED_FRAMEWORK_NAME/$TARGET_COREFX_VERSION/