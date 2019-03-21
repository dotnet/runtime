#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#

# Dockerfile that creates a container suitable to build dotnet-cli
FROM centos:7.1.1503

# Swap the "fakesystemd" package with the real "systemd" package, because fakesystemd conflicts with openssl-devel.
# The CentOS Docker image uses fakesystemd instead of systemd to reduce disk space.
RUN yum -q -y swap -- remove fakesystemd -- install systemd systemd-libs

RUN yum -q -y install deltarpm
RUN yum -q -y install epel-release
# RUN yum -y update

# This could become a "microsoft/coreclr" image, since it just installs the dependencies for CoreCLR (and stdlib)
# Install CoreCLR and CoreFx dependencies
RUN yum -q -y install unzip libunwind gettext libcurl-devel openssl-devel zlib libicu-devel

# RUN apt-get update && \
#     apt-get -qqy install unzip curl libicu-dev libunwind8 gettext libssl-dev libcurl3-gnutls zlib1g liblttng-ust-dev lldb-3.6-dev lldb-3.6 

# Install Build Prereqs
# CMake 3.3.2 from GhettoForge; LLVM 3.6.2 built from source ourselves;
RUN yum install -y http://mirror.symnds.com/distributions/gf/el/7/plus/x86_64/cmake-3.3.2-1.gf.el7.x86_64.rpm \
        https://matell.blob.core.windows.net/rpms/clang-3.6.2-1.el7.centos.x86_64.rpm \
        https://matell.blob.core.windows.net/rpms/clang-libs-3.6.2-1.el7.centos.x86_64.rpm \
        https://matell.blob.core.windows.net/rpms/lldb-3.6.2-1.el7.centos.x86_64.rpm \
        https://matell.blob.core.windows.net/rpms/lldb-devel-3.6.2-1.el7.centos.x86_64.rpm \
        https://matell.blob.core.windows.net/rpms/llvm-3.6.2-1.el7.centos.x86_64.rpm \
        https://matell.blob.core.windows.net/rpms/llvm-libs-3.6.2-1.el7.centos.x86_64.rpm \
        which \
        make

RUN yum -q -y install tar git

# Use clang as c++ compiler
RUN update-alternatives --install /usr/bin/c++ c++ /usr/bin/clang++ 100
RUN update-alternatives --set c++ /usr/bin/clang++

RUN yum -q -y install sudo

# Setup User to match Host User, and give superuser permissions
ARG USER_ID=0
RUN useradd -m code_executor -u ${USER_ID} -g root
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# With the User Change, we need to change permssions on these directories
RUN chmod -R a+rwx /usr/local
RUN chmod -R a+rwx /home
RUN chmod -R 755 /usr/bin/sudo

# Set user to the one we just created
USER ${USER_ID}

# Set working directory
WORKDIR /opt/code
