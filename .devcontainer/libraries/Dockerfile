# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.192.0/containers/dotnet/.devcontainer/base.Dockerfile
# For details on dotnet specific container, see: https://github.com/microsoft/vscode-dev-containers/tree/main/containers/dotnet

# [Choice] .NET version: 6.0, 3.1
ARG VARIANT="6.0-focal"
FROM mcr.microsoft.com/devcontainers/dotnet:0-${VARIANT}

# Set up machine requirements to build the repo and the gh CLI
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        cmake \
        llvm-10 \
        clang-10 \
        build-essential \
        python \
        curl \
        git \
        lldb-6.0 \
        liblldb-6.0-dev \
        libunwind8 \
        libunwind8-dev \
        gettext \
        libicu-dev \
        liblttng-ust-dev \
        libssl-dev \
        libkrb5-dev \
        zlib1g-dev \
        ninja-build
