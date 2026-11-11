# See here for image contents: https://github.com/devcontainers/images/tree/main/src/dotnet

ARG VARIANT="11.0-resolute"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

SHELL [ "/bin/bash", "-c" ]

# Set up machine requirements using the scripts from this checkout.
COPY eng/common/native/install-dependencies.sh eng/common/native/init-os-and-arch.sh /tmp/runtime-dependencies/
RUN bash /tmp/runtime-dependencies/install-dependencies.sh && \
  rm /tmp/runtime-dependencies/{install-dependencies,init-os-and-arch}.sh && \
  rmdir /tmp/runtime-dependencies
