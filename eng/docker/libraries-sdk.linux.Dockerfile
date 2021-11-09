# Builds and copies library artifacts into target dotnet sdk image
ARG BUILD_BASE_IMAGE=mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-f39df28-20191023143754
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:6.0-bullseye-slim

FROM $BUILD_BASE_IMAGE as corefxbuild
ARG CONFIGURATION=Release
WORKDIR /repo
COPY . .

RUN ./build.sh clr+libs -runtimeconfiguration Release -configuration $CONFIGURATION -ci

# Mocks:
# RUN mkdir -p /repo/artifacts/bin/testhost/net7.0-Linux-Release-x64
# RUN echo 'lol' > /repo/artifacts/bin/testhost/lol.txt
# 
# RUN mkdir -p /repo/artifacts/bin/microsoft.netcore.app.ref
# RUN echo 'rofl' > /repo/artifacts/bin/microsoft.netcore.app.ref/rofl.txt
# 
# RUN mkdir -p /repo/artifacts/bin/microsoft.netcore.app.runtime.linux-x64
# RUN echo 'haha' > /repo/artifacts/bin/microsoft.netcore.app.runtime.linux-x64/haha.txt

FROM $SDK_BASE_IMAGE as target

# Install 7.0 SDK:
RUN wget https://dot.net/v1/dotnet-install.sh
RUN bash ./dotnet-install.sh --channel 7.0.1xx --quality daily --install-dir /usr/share/dotnet

## Collect the following artifacts under /live-runtime-artifacts,
## so projects can build and test against the live-built runtime:
## 1. Reference assembly pack (microsoft.netcore.app.ref)
## 2. Runtime pack (microsoft.netcore.app.runtime.linux-x64)
## 3. targetingpacks.targets, so stress test builds can target the live-built runtime instead of the one in the pre-installed SDK
## 4. testhost

ARG VERSION=7.0
ARG ARCH=x64
ARG CONFIGURATION=Release

COPY --from=corefxbuild \
    /repo/artifacts/bin/microsoft.netcore.app.ref \
    /live-runtime-artifacts/microsoft.netcore.app.ref

COPY --from=corefxbuild \
    /repo/artifacts/bin/microsoft.netcore.app.runtime.linux-$ARCH \
    /live-runtime-artifacts/microsoft.netcore.app.runtime.linux-$ARCH

COPY --from=corefxbuild \
    /repo/eng/targetingpacks.targets \
    /live-runtime-artifacts/targetingpacks.targets

COPY --from=corefxbuild \
    /repo/artifacts/bin/testhost \
    /live-runtime-artifacts/testhost

# Add AspNetCore bits to testhost:
RUN mkdir -p /live-runtime-artifacts/testhost/net$VERSION-Linux-$CONFIGURATION-$ARCH/shared/Microsoft.AspNetCore.App
RUN cp -r /usr/share/dotnet/shared/Microsoft.AspNetCore.App/$VERSION* /live-runtime-artifacts/testhost/net$VERSION-Linux-$CONFIGURATION-$ARCH/shared/Microsoft.AspNetCore.App