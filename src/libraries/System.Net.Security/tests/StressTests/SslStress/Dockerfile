ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:6.0-bullseye-slim
FROM $SDK_BASE_IMAGE

RUN echo "DOTNET_SDK_VERSION="$DOTNET_SDK_VERSION
RUN echo "DOTNET_VERSION="$DOTNET_VERSION

WORKDIR /app
COPY . .
WORKDIR /app/System.Net.Security/tests/StressTests/SslStress

ARG VERSION=7.0
ARG CONFIGURATION=Release
ARG ARCH=x64

RUN dotnet build -c $CONFIGURATION \
    -p:TargetingPacksTargetsLocation=/live-runtime-artifacts/targetingpacks.targets \
    -p:MicrosoftNetCoreAppRefPackDir=/live-runtime-artifacts/microsoft.netcore.app.ref/ \
    -p:MicrosoftNetCoreAppRuntimePackDir=/live-runtime-artifacts/microsoft.netcore.app.runtime.linux-$ARCH/$CONFIGURATION/

EXPOSE 5001

ENV VERSION=$VERSION
ENV CONFIGURATION=$CONFIGURATION
ENV ARCH=$ARCH
ENV SSLSTRESS_ARGS=''

CMD /live-runtime-artifacts/testhost/net$VERSION-Linux-$CONFIGURATION-$ARCH/dotnet exec ./bin/$CONFIGURATION/net$VERSION/SslStress.dll $SSLSTRESS_ARGS