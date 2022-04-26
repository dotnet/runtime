ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:6.0-bullseye-slim
FROM $SDK_BASE_IMAGE

WORKDIR /msquic

# Build latest msquic locally
RUN apt-get update -y
RUN apt-get upgrade -y
RUN apt-get install -y cmake clang ruby-dev gem lttng-tools libssl-dev && \
    gem install fpm
RUN git clone --recursive https://github.com/dotnet/msquic
RUN cd msquic/src/msquic && \
    mkdir build && \
    options="-DQUIC_BUILD_TOOLS=off -DQUIC_BUILD_TEST=off -DQUIC_BUILD_PERF=off" && \
    cmake -B build -DCMAKE_BUILD_TYPE=Release -DQUIC_ENABLE_LOGGING=false -DQUIC_USE_SYSTEM_LIBCRYPTO=true -DQUIC_BUILD_TOOLS=off -DQUIC_BUILD_TEST=off -DQUIC_BUILD_PERF=off && \
    cd build && \
    cmake --build . --config Release
RUN cd msquic/src/msquic/build/bin/Release && \
    rm libmsquic.so && \
    fpm -f -s dir -t deb -n libmsquic -v $( find -type f | cut -d "." -f 4- ) \
      --license MIT --url https://github.com/microsoft/msquic --log error \
      $( ls ./* | cut -d "/" -f 2 | sed -r "s/(.*)/\1=\/usr\/lib\/\1/g" ) && \
      dpkg -i libmsquic_*.deb

WORKDIR /app
COPY . .

ARG VERSION=7.0
ARG CONFIGURATION=Release

RUN dotnet build -c $CONFIGURATION \
    -p:TargetingPacksTargetsLocation=/live-runtime-artifacts/targetingpacks.targets \
    -p:MicrosoftNetCoreAppRefPackDir=/live-runtime-artifacts/microsoft.netcore.app.ref/ \
    -p:MicrosoftNetCoreAppRuntimePackDir=/live-runtime-artifacts/microsoft.netcore.app.runtime.linux-x64/$CONFIGURATION/

# Enable dump collection
ENV COMPlus_DbgEnableMiniDump=1
ENV COMPlus_DbgMiniDumpType=MiniDumpWithFullMemory
ENV COMPlus_DbgMiniDumpName="/dumps-share/coredump.%p"

EXPOSE 5001

ENV VERSION=$VERSION
ENV CONFIGURATION=$CONFIGURATION
ENV HTTPSTRESS_ARGS=''
CMD /live-runtime-artifacts/testhost/net$VERSION-Linux-$CONFIGURATION-x64/dotnet exec \
    ./bin/$CONFIGURATION/net$VERSION/HttpStress.dll $HTTPSTRESS_ARGS
