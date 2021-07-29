ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:6.0-bullseye-slim
FROM $SDK_BASE_IMAGE

RUN echo "DOTNET_SDK_VERSION="$DOTNET_SDK_VERSION
RUN echo "DOTNET_VERSION="$DOTNET_VERSION

WORKDIR /app
COPY . .

# Pulling the msquic Debian package from msquic-ci public pipeline and from a hardcoded build.
# Note that this is a temporary solution until we have properly published Linux packages.
# Also note that in order to update to a newer msquic build, you have update this link.
ARG MSQUIC_PACKAGE=libmsquic_1.5.0_amd64.deb
ARG PACKAGES_DIR=LinuxPackages
RUN wget 'https://dev.azure.com/dnceng/9ee6d478-d288-47f7-aacc-f6e6d082ae6d/_apis/build/builds/1223883/artifacts?artifactName=LinuxPackages&api-version=6.0&%24format=zip' -O "$PACKAGES_DIR".zip
RUN apt-get update
RUN apt-get install unzip
RUN unzip $PACKAGES_DIR.zip
RUN dpkg -i $PACKAGES_DIR/$MSQUIC_PACKAGE
RUN rm -rf $PACKAGES_DIR*

ARG CONFIGURATION=Release
RUN dotnet build -c $CONFIGURATION

EXPOSE 5001

ENV CONFIGURATION=$CONFIGURATION
ENV HTTPSTRESS_ARGS=''
CMD dotnet run --no-build -c $CONFIGURATION -- $HTTPSTRESS_ARGS
