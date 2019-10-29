# Builds a dotnet sdk base image which contains corefx bits compiled from source
ARG BUILD_BASE_IMAGE=mcr.microsoft.com/dotnet-buildtools/prereqs:rhel7_prereqs_2
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/core/sdk:3.0.100-buster

FROM $BUILD_BASE_IMAGE as corefxbuild

WORKDIR /repo
COPY . .

ARG CONFIGURATION=Release
RUN ./build.sh -c $CONFIGURATION

FROM $SDK_BASE_IMAGE as target

ARG TESTHOST_LOCATION=/repo/artifacts/bin/testhost
ARG TFM=netcoreapp
ARG OS=Linux
ARG ARCH=x64
ARG CONFIGURATION=Release

ARG COREFX_SHARED_FRAMEWORK_NAME=Microsoft.NETCore.App
ARG SOURCE_COREFX_VERSION=5.0.0
ARG TARGET_SHARED_FRAMEWORK=/usr/share/dotnet/shared
ARG TARGET_COREFX_VERSION=3.0.0

COPY --from=corefxbuild \
    $TESTHOST_LOCATION/$TFM-$OS-$CONFIGURATION-$ARCH/shared/$COREFX_SHARED_FRAMEWORK_NAME/$SOURCE_COREFX_VERSION/* \
    $TARGET_SHARED_FRAMEWORK/$COREFX_SHARED_FRAMEWORK_NAME/$TARGET_COREFX_VERSION/
