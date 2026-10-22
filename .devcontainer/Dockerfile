# See here for image contents: https://github.com/devcontainers/images/tree/main/src/dotnet

ARG VARIANT="11.0-resolute"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

SHELL [ "/bin/bash", "-c" ]

# Install the Linux dependencies from eng/common/native/install-dependencies.sh.
# Ubuntu 26.04 no longer supplies the locale.alias file used by that script.
RUN apt-get update && \
  apt-get install -y build-essential gettext locales cmake llvm clang lld lldb liblldb-dev libunwind8-dev libicu-dev liblttng-ust-dev \
    libssl-dev libkrb5-dev pigz cpio ninja-build file && \
  localedef -i en_US -c -f UTF-8 en_US.UTF-8 && \
  apt-get clean
