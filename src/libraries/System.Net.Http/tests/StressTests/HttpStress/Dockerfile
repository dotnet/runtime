ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:8.0
FROM $SDK_BASE_IMAGE

# Add MsQuic
RUN apt-get update && apt-get install -y gnupg && \
    curl -LO https://packages.microsoft.com/keys/microsoft.asc && \
    echo 2cfd20a306b2fa5e25522d78f2ef50a1f429d35fd30bd983e2ebffc2b80944fa microsoft.asc| sha256sum --check - && \
    apt-key add microsoft.asc && \
    rm microsoft.asc && \
    echo deb https://packages.microsoft.com/debian/12/prod bookworm main >> /etc/apt/sources.list.d/microsoft.list && \
    apt-get update && \
    apt-get install -y libmsquic && \
    rm -rf /var/lib/apt/lists/*

ARG VERSION=9.0
ARG CONFIGURATION=Release

# Build the stress server
WORKDIR /app
COPY . .

RUN dotnet build -c $CONFIGURATION \
    -p:MsQuicInteropIncludes="/live-runtime-artifacts/msquic-interop/*.cs" \
    -p:TargetingPacksTargetsLocation=/live-runtime-artifacts/targetingpacks.targets \
    -p:MicrosoftNetCoreAppRefPackDir=/live-runtime-artifacts/microsoft.netcore.app.ref/ \
    -p:MicrosoftNetCoreAppRuntimePackDir=/live-runtime-artifacts/microsoft.netcore.app.runtime.linux-x64/$CONFIGURATION/

# Enable dump collection
ENV DOTNET_DbgEnableMiniDump=1
ENV DOTNET_DbgMiniDumpType=MiniDumpWithFullMemory
ENV DOTNET_DbgMiniDumpName="/dumps-share/coredump.%p"

EXPOSE 5001

ENV VERSION=$VERSION
ENV CONFIGURATION=$CONFIGURATION
ENV HTTPSTRESS_ARGS=''
CMD /live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/dotnet exec --roll-forward Major \
    ./bin/$CONFIGURATION/net$VERSION/HttpStress.dll $HTTPSTRESS_ARGS
