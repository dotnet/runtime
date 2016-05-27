#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

FROM ubuntu:16.04

# Install the base toolchain we need to build anything (clang, cmake, make and the like)
# this does not include libraries that we need to compile different projects, we'd like
# them in a different layer.
RUN apt-get update && \
    apt-get install -y wget && \
    echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.6 main" | tee /etc/apt/sources.list.d/llvm.list && \
    wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | apt-key add - && \
    apt-get update && \
    apt-get install -y cmake \
            make \
            llvm-3.5 \
            clang-3.5 \
            lldb-3.6 \
            lldb-3.6-dev && \
    apt-get clean

# Install tools used by the VSO build automation.  nodejs-legacy is a Debian specific
# package that provides `node' on the path (which azure cli needs).
RUN apt-get install -y git \
            zip \
            curl \
            tar \
            nodejs \
            nodejs-legacy \
            npm && \
    apt-get clean && \
    npm install -g azure-cli && \
    npm cache clean

# Dependencies for CoreCLR and CoreFX
RUN apt-get install -y gettext \
            libunwind8-dev \
            libkrb5-dev \
            libunwind8 \
            libicu-dev \
            liblttng-ust-dev \
            libcurl4-openssl-dev \
            libssl-dev \
            uuid-dev && \
    apt-get clean

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