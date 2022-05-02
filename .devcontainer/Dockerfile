# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.192.0/containers/dotnet/.devcontainer/base.Dockerfile

# [Choice] .NET version: 5.0, 3.1, 2.1
ARG VARIANT="5.0"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:0-${VARIANT}

# Set up machine requirements to build the repo
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends cmake llvm-9 clang-9 \
        build-essential python curl git lldb-6.0 liblldb-6.0-dev \
        libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev \
        libssl-dev libnuma-dev libkrb5-dev zlib1g-dev ninja-build

# Install V8 Engine
SHELL ["/bin/bash", "-c"]

RUN curl -sSL "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/linux/chromium-v8/v8-linux64-rel-8.5.183.zip" -o ./v8.zip \
    && unzip ./v8.zip -d /usr/local/v8 \
    && echo $'#!/usr/bin/env bash\n\
"/usr/local/v8/d8" --snapshot_blob="/usr/local/v8/snapshot_blob.bin" "$@"\n' > /usr/local/bin/v8 \
    && chmod +x /usr/local/bin/v8

# install chromium dependencies to run debugger tests:
RUN sudo apt-get install libnss3 -y \
    && apt-get install libatk1.0-0 -y \
    && apt-get install libatk-bridge2.0-0 -y \
    && apt-get install libcups2 -y \
    && apt-get install libdrm2 -y \
    && apt-get install libxkbcommon-x11-0 -y \
    && apt-get install libxcomposite-dev -y \
    && apt-get install libxdamage1 -y \
    && apt-get install libxrandr2 -y \
    && apt-get install libgbm-dev -y \
    && apt-get install libpango-1.0-0 -y \
    && apt-get install libcairo2 -y \
    && apt-get install libasound2 -y 

#install firefox dependecies to run debugger tests:
RUN sudo apt-get install libdbus-glib-1-2 -y \
    && apt-get install libgtk-3-0 -y \
    && apt-get install libx11-xcb-dev  -y