# Builds and copies library artifacts into target dotnet sdk image
ARG BUILD_BASE_IMAGE=mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:7.0-bullseye-slim

FROM $BUILD_BASE_IMAGE as corefxbuild

# Enable openssl legacy provider in system openssl config
RUN fixOpensslConf=$(mktemp) && \
    printf "#!/usr/bin/env sh\n\
        sed -i '\n\
            # Append 'legacy = legacy_sect' after 'default = default_sect' under [provider_sect]
            /^default = default_sect/a legacy = legacy_sect\n\
            # Search for [default_sect]
            /\[default_sect\]/ {\n\
                # Go to next line
                n\n\
                # Uncomment '# activate = 1'
                s/# //\n\
                # Append new [legacy_sect], with 'activate = 1'
                a\n\
                a [legacy_sect]\n\
                a activate = 1\n\
            }\n\
            ' /etc/ssl/openssl.cnf\n" > $fixOpensslConf && \
    sh $fixOpensslConf && \
    rm $fixOpensslConf

ARG CONFIGURATION=Release

WORKDIR /repo
COPY . .
RUN ./build.sh clr+libs -runtimeconfiguration Release -configuration $CONFIGURATION -ci

FROM $SDK_BASE_IMAGE as target

ARG VERSION=8.0
ARG CONFIGURATION=Release
ENV _DOTNET_INSTALL_CHANNEL="$VERSION.1xx"

# Install latest daily SDK:
RUN wget https://dot.net/v1/dotnet-install.sh
RUN bash ./dotnet-install.sh --channel $_DOTNET_INSTALL_CHANNEL --quality daily --install-dir /usr/share/dotnet

# Collect the following artifacts under /live-runtime-artifacts,
# so projects can build and test against the live-built runtime:
# 1. Reference assembly pack (microsoft.netcore.app.ref)
# 2. Runtime pack (microsoft.netcore.app.runtime.linux-x64)
# 3. targetingpacks.targets, so stress test builds can target the live-built runtime instead of the one in the pre-installed SDK
# 4. testhost

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

# Add AspNetCore bits to testhost:
ENV _ASPNETCORE_SOURCE="/usr/share/dotnet/shared/Microsoft.AspNetCore.App/$VERSION*"
ENV _ASPNETCORE_DEST="/live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/shared/Microsoft.AspNetCore.App"
RUN mkdir -p $_ASPNETCORE_DEST
RUN cp -r $_ASPNETCORE_SOURCE $_ASPNETCORE_DEST