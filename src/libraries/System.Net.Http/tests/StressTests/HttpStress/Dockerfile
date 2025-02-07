ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:latest
FROM $SDK_BASE_IMAGE

# Build latest msquic locally
WORKDIR /msquic
RUN apt-get update -y && \
    apt-get upgrade -y && \
    apt-get install -y cmake clang ruby-dev gem lttng-tools libssl-dev && \
    gem install fpm
RUN git clone --depth 1 --single-branch --branch v2.4.4 --recursive https://github.com/microsoft/msquic
RUN cd msquic/ && \
    mkdir build && \
    cmake -B build -DCMAKE_BUILD_TYPE=Debug -DQUIC_ENABLE_LOGGING=false -DQUIC_USE_SYSTEM_LIBCRYPTO=true -DQUIC_BUILD_TOOLS=off -DQUIC_BUILD_TEST=off -DQUIC_BUILD_PERF=off -DQUIC_TLS=openssl3 && \
      # -DQUIC_ENABLE_SANITIZERS=on && \
    cd build && \
    cmake --build . --config Debug
RUN cd msquic/build/bin/Debug && \
    rm libmsquic.so && \
    fpm -f -s dir -t deb -n libmsquic -v $( find -type f | cut -d "." -f 4- ) \
    --license MIT --url https://github.com/microsoft/msquic --log error \
    $( ls ./* | cut -d "/" -f 2 | sed -r "s/(.*)/\1=\/usr\/lib\/\1/g" ) && \
    dpkg -i libmsquic_*.deb

ARG VERSION
ARG CONFIGURATION

# Build the stress server
WORKDIR /app
COPY . .

RUN dotnet build -c $CONFIGURATION \
    -p:NetCoreAppCurrentVersion=$VERSION \
    -p:MsQuicInteropIncludes="/live-runtime-artifacts/msquic-interop/*.cs" \
    -p:TargetingPacksTargetsLocation=/live-runtime-artifacts/targetingpacks.targets \
    -p:MicrosoftNetCoreAppRefPackDir=/live-runtime-artifacts/microsoft.netcore.app.ref/ \
    -p:MicrosoftNetCoreAppRuntimePackDir=/live-runtime-artifacts/microsoft.netcore.app.runtime.linux-x64/$CONFIGURATION/

EXPOSE 5001

ENV VERSION=$VERSION
ENV CONFIGURATION=$CONFIGURATION
ENV STRESS_ROLE=''
ENV STRESS_ARGS=''

# configure adress sanitizer
# ENV ASAN_OPTIONS='detect_leaks=0'
# ENV LD_PRELOAD=/usr/lib/gcc/x86_64-linux-gnu/12/libasan.so

CMD ./entrypoint.sh
