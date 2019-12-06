# Switch to mcr.microsoft.com/dotnet-buildtools/prereqs ubuntu 18.04 images when they are fixed
FROM ubuntu:18.04

# Prevents dialog prompting when installing packages
ARG DEBIAN_FRONTEND=noninteractive

# This 'RUN' step can be removed once dotnet-buildtools/prereqs image is fixed
#
# Makes the image capable of building and running tests in dotnet-runtime repo.
# Add retries to apt-get since the ubuntu package servers have had errors lately such as:
#   "E: Failed to fetch http://archive.ubuntu.com/ubuntu/pool/main/p/publicsuffix/publicsuffix_20180223.1310-1_all.deb  Undetermined Error [IP: 91.189.88.31 80]"
ARG APT_KEY_DONT_WARN_ON_DANGEROUS_USAGE=1
RUN echo "APT::Acquire::Retries \"10\";" > /etc/apt/apt.conf.d/80-retries && \
    apt-get update && \
    apt-get install -y --no-install-recommends apt-transport-https ca-certificates gnupg software-properties-common wget && \
    wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | apt-key add - && \
    apt-add-repository 'deb https://apt.kitware.com/ubuntu/ bionic main' && \
    wget -O - https://apt.llvm.org/llvm-snapshot.gpg.key 2>/dev/null | apt-key add - && \
    apt-add-repository 'deb http://apt.llvm.org/bionic/ llvm-toolchain-bionic-9 main' && \
    apt-get update && \
    apt-get install -y --no-install-recommends cmake llvm-9 clang-9 lldb-6.0 liblldb-6.0-dev libunwind8 libunwind-dev gettext libicu-dev liblttng-ust-dev libssl-dev libnuma-dev libkrb5-dev locales && \
    locale-gen en_US.UTF-8 && \
    update-locale LANG=en_US.UTF-8

# Install Kerberos, NTLM, and diagnostic tools
COPY ./common/krb5.conf /etc/krb5.conf
RUN apt-get update && \
    apt-get install -y --no-install-recommends krb5-user gss-ntlmssp iputils-ping dnsutils nano curl

# Set environment variable to turn on enterprise tests
ENV DOTNET_RUNTIME_ENTERPRISETESTS_ENABLED 1

WORKDIR /setup
COPY ./linuxclient/*.sh ./
RUN chmod +x *.sh

WORKDIR /repo

ENTRYPOINT ["/bin/bash", "/setup/run.sh"]
