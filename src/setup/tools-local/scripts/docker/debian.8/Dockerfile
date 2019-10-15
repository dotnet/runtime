#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#

# Dockerfile that creates a container suitable to build dotnet-cli
FROM debian:jessie

# Misc Dependencies for build
RUN apt-get update && \
    apt-get -qqy install \
        curl \
        wget \
        unzip \
        gettext \
        sudo && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# This could become a "microsoft/coreclr" image, since it just installs the dependencies for CoreCLR (and stdlib)
RUN apt-get update &&\
    apt-get -qqy install \
        libunwind8 \
        libkrb5-3 \
        libicu52 \
        liblttng-ust0 \
        libssl1.0.0 \
        zlib1g \
        libuuid1 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Install Build Prereqs
RUN apt-get update && \
    apt-get -qqy install \
        debhelper \
        build-essential \
        devscripts \
        git \
        cmake \
        clang-3.5 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# liblldb is needed so deb package build does not throw missing library info errors
RUN wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key|sudo apt-key add -
RUN echo "deb http://llvm.org/apt/jessie/ llvm-toolchain-jessie-3.6 main" > /etc/apt/sources.list.d/llvm-toolchain.list
RUN apt-get update && \
    apt-get -qqy install liblldb-3.6 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Use clang as c++ compiler
RUN update-alternatives --install /usr/bin/c++ c++ /usr/bin/clang++-3.5 100
RUN update-alternatives --set c++ /usr/bin/clang++-3.5

# Setup User to match Host User, and give superuser permissions
ARG USER_ID=0
RUN useradd -m code_executor -u ${USER_ID} -g sudo
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# With the User Change, we need to change permissions on these directories
RUN chmod -R a+rwx /usr/local
RUN chmod -R a+rwx /home
RUN chmod -R 755 /usr/lib/sudo

# Set user to the one we just created
USER ${USER_ID}

# Set working directory
WORKDIR /opt/code

# Work around https://github.com/dotnet/cli/issues/1582 until Docker releases a
# fix (https://github.com/docker/docker/issues/20818). This workaround allows
# the container to be run with the default seccomp Docker settings by avoiding
# the restart_syscall made by LTTng which causes a failed assertion.
ENV LTTNG_UST_REGISTER_TIMEOUT 0
