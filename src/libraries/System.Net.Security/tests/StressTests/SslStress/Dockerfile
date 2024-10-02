ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:latest
FROM $SDK_BASE_IMAGE

WORKDIR /app
COPY . .
WORKDIR /app/System.Net.Security/tests/StressTests/SslStress

ARG VERSION=9.0
ARG CONFIGURATION=Release

RUN dotnet build -c $CONFIGURATION \
    -p:NetCoreAppCurrentVersion=$VERSION \
    -p:TargetingPacksTargetsLocation=/live-runtime-artifacts/targetingpacks.targets \
    -p:MicrosoftNetCoreAppRefPackDir=/live-runtime-artifacts/microsoft.netcore.app.ref/ \
    -p:MicrosoftNetCoreAppRuntimePackDir=/live-runtime-artifacts/microsoft.netcore.app.runtime.linux-x64/$CONFIGURATION/

EXPOSE 5001

ENV VERSION=$VERSION
ENV CONFIGURATION=$CONFIGURATION
ENV STRESS_ROLE=''
ENV STRESS_ARGS=''

CMD ./entrypoint.sh
