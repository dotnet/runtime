# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.192.0/containers/dotnet/.devcontainer/base.Dockerfile
# For details on dotnet specific container, see: https://github.com/microsoft/vscode-dev-containers/tree/main/containers/dotnet

ARG VARIANT="8.0-noble"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

SHELL [ "/bin/bash", "-c" ]

# Set up machine requirements to build the repo and the gh CLI.
RUN curl --remote-name-all -sSL https://github.com/dotnet/runtime/raw/main/eng/common/native/{install-dependencies,init-os-and-arch}.sh && \
  bash install-dependencies.sh && rm {install-dependencies,init-os-and-arch}.sh
