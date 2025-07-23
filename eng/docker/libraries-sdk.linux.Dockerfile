# Builds and copies library artifacts into target dotnet sdk image
ARG BUILD_BASE_IMAGE=mcr.microsoft.com/dotnet-buildtools/prereqs:centos-stream9
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:latest

FROM $BUILD_BASE_IMAGE as corefxbuild

ARG CONFIGURATION=Release

WORKDIR /repo
COPY . .
RUN ./build.sh clr+libs -runtimeconfiguration Release -configuration $CONFIGURATION -ci

FROM $SDK_BASE_IMAGE as target

ARG VERSION
ARG CONFIGURATION=Release
ENV _DOTNET_INSTALL_CHANNEL=$VERSION

# remove the existing SDK, we want to start from a clean slate with latest daily
RUN rm -rf /usr/share/dotnet

# Install latest daily SDK:
RUN wget https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh
RUN bash ./dotnet-install.sh --channel $_DOTNET_INSTALL_CHANNEL --quality daily --install-dir /usr/share/dotnet

# Collect the following artifacts under /live-runtime-artifacts,
# so projects can build and test against the live-built runtime:
# 1. Reference assembly pack (microsoft.netcore.app.ref)
# 2. Runtime pack (microsoft.netcore.app.runtime.linux-x64)
# 3. targetingpacks.targets, so stress test builds can target the live-built runtime instead of the one in the pre-installed SDK
# 4. testhost
# 5. msquic interop sources (needed for HttpStress)

COPY --from=corefxbuild \
    /repo/artifacts/bin/microsoft.netcore.app.ref \
    /live-runtime-artifacts/microsoft.netcore.app.ref

COPY --from=corefxbuild \
    /repo/artifacts/bin/microsoft.netcore.app.runtime.linux-x64 \
    /live-runtime-artifacts/microsoft.netcore.app.runtime.linux-x64

COPY --from=corefxbuild \
    /repo/eng/targetingpacks.targets \
    /live-runtime-artifacts/targetingpacks.targets

COPY --from=corefxbuild \
    /repo/artifacts/bin/testhost \
    /live-runtime-artifacts/testhost

COPY --from=corefxbuild \
    /repo/src/libraries/System.Net.Quic/src/System/Net/Quic/Interop \
    /live-runtime-artifacts/msquic-interop

# Add AspNetCore bits to testhost, there should be only one version since we started from an image without existing SDK:
ENV _ASPNETCORE_SOURCE="/usr/share/dotnet/shared/Microsoft.AspNetCore.App/*"
ENV _ASPNETCORE_DEST="/live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/shared/Microsoft.AspNetCore.App"
RUN mkdir -p $_ASPNETCORE_DEST
RUN cp -r $_ASPNETCORE_SOURCE $_ASPNETCORE_DEST
