# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.192.0/containers/dotnet/.devcontainer/base.Dockerfile
# For details on dotnet specific container, see: https://github.com/microsoft/vscode-dev-containers/tree/main/containers/dotnet

# [Choice] .NET version: 6.0, 7.0
ARG VARIANT="6.0-jammy"
FROM mcr.microsoft.com/devcontainers/dotnet:0-${VARIANT}

# Set up machine requirements to build the repo and the gh CLI
# Clang-16 up is rquired but Ubuntu 22.04 comes with clang-14 highest, so add clang-18 sources
RUN apt-get update \
    && wget -O - https://apt.llvm.org/llvm-snapshot.gpg.key | sudo apt-key add - \
    && apt-get install software-properties-common -y \
    && add-apt-repository "deb http://apt.llvm.org/$(lsb_release -s -c)/ llvm-toolchain-$(lsb_release -s -c)-18 main" -y \
    && apt-get update \
    && apt-get install clang-18 -y

# Set up machine requirements to build the repo and the gh CLI
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        cmake \
        build-essential \
        python3 \
        curl \
        git \
        lldb \
        liblldb-dev \
        libunwind8 \
        libunwind8-dev \
        gettext \
        libicu-dev \
        liblttng-ust-dev \
        libssl-dev \
        libkrb5-dev \
        zlib1g-dev \
        ninja-build \
        tzdata
