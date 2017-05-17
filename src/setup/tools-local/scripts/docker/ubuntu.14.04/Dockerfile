#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# Dockerfile that creates a container suitable to build dotnet-cli
FROM ubuntu:14.04

# Misc Dependencies for build
RUN apt-get update && \
    apt-get -qqy install \
        curl \
        unzip \
        gettext \
        sudo && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# This could become a "microsoft/coreclr" image, since it just installs the dependencies for CoreCLR (and stdlib)
RUN apt-get update && \
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
        clang-3.5 \
        lldb-3.6 && \
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
