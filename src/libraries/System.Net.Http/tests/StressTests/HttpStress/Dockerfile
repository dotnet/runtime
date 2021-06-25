# use nightly build of 6.0 from main (Debian)
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:latest
FROM $SDK_BASE_IMAGE

RUN echo "DOTNET_SDK_VERSION="$DOTNET_SDK_VERSION
RUN echo "DOTNET_VERSION="$DOTNET_VERSION

# install dependencies of libmsquic.so
RUN apt-get update -y \
    && apt-get install -y \
        liblttng-ust0 \
        libatomic1 \
        libc6 \
    && apt-get clean

WORKDIR /app
COPY . .

# pre-built msquic should be inside stress tests directory so we can copy it to container here
COPY libmsquic.so /usr/share/dotnet/shared/Microsoft.NETCore.App/$DOTNET_VERSION/
COPY libmsquic.lttng.so /usr/share/dotnet/shared/Microsoft.NETCore.App/$DOTNET_VERSION/

# no need to rebuild whole clr+libs with "-b" argument, we can substitute specific dlls only
# for that, pre-build locally for net6.0-Linux-Release, put inside stress tests directory and uncomment copy below
# COPY System.Net.Quic.dll /usr/share/dotnet/shared/Microsoft.NETCore.App/$DOTNET_VERSION/
# COPY System.Net.Http.dll /usr/share/dotnet/shared/Microsoft.NETCore.App/$DOTNET_VERSION/

ARG CONFIGURATION=Release
RUN dotnet build -c $CONFIGURATION

EXPOSE 5001

ENV CONFIGURATION=$CONFIGURATION
ENV HTTPSTRESS_ARGS=''
CMD dotnet run --no-build -c $CONFIGURATION -- $HTTPSTRESS_ARGS
