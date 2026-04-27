# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.192.0/containers/dotnet/.devcontainer/base.Dockerfile
# For details on dotnet specific container, see: https://github.com/microsoft/vscode-dev-containers/tree/main/containers/dotnet

ARG VARIANT="8.0-noble"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

# Set up machine requirements to build the repo and the gh CLI.
# Keep this package list in sync with docs/workflow/requirements/linux-requirements.md.
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        build-essential \
        clang \
        cmake \
        cpio \
        curl \
        gettext \
        git \
        libicu-dev \
        libkrb5-dev \
        liblldb-dev \
        liblttng-ust-dev \
        libssl-dev \
        libunwind8 \
        libunwind8-dev \
        lld \
        lldb \
        llvm \
        ninja-build \
        pigz \
        python-is-python3 \
        python3 \
        tzdata
