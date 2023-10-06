ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:8.0
FROM $SDK_BASE_IMAGE

WORKDIR /app
COPY . .
WORKDIR /app/System.Net.Security/tests/StressTests/SslStress

ARG VERSION=9.0
ARG CONFIGURATION=Release

RUN dotnet build -c $CONFIGURATION \
    -p:TargetingPacksTargetsLocation=/live-runtime-artifacts/targetingpacks.targets \
    -p:MicrosoftNetCoreAppRefPackDir=/live-runtime-artifacts/microsoft.netcore.app.ref/ \
    -p:MicrosoftNetCoreAppRuntimePackDir=/live-runtime-artifacts/microsoft.netcore.app.runtime.linux-x64/$CONFIGURATION/

EXPOSE 5001

ENV VERSION=$VERSION
ENV CONFIGURATION=$CONFIGURATION
ENV SSLSTRESS_ARGS=''

CMD /live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/dotnet exec --roll-forward Major \
    ./bin/$CONFIGURATION/net$VERSION/SslStress.dll $SSLSTRESS_ARGS