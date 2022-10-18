FROM ubuntu:18.04

RUN apt-get update && apt-get -y install \
    # .NET Core SDK needs: https://docs.microsoft.com/en-us/dotnet/core/install/dependencies?pivots=os-linux&tabs=netcore31#supported-operating-systems
    curl libcurl4 libssl1.0.0 zlib1g libicu60 libkrb5-3 liblttng-ust0 \
    # Tests need:
    build-essential gcc

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 3.0.100

ENV PATH="/root/.dotnet:${PATH}"
RUN dotnet --info

WORKDIR /src

CMD dotnet test