# 
# Copyright (c) .NET Foundation and contributors. All rights reserved. 
# Licensed under the MIT license. See LICENSE file in the project root for full license information. 
# 
 
# Dockerfile that creates a container suitable to build dotnet-cli 
FROM chcosta/dotnetcore:fedora23_prereqs 
 
# Install the base toolchain we need to build anything (clang, cmake, make and the like) 
# this does not include libraries that we need to compile different projects, we'd like 
# them in a different layer. 
RUN dnf install -y cmake \ 
        clang \ 
        lldb-devel \ 
        make \ 
        which && \ 
    dnf clean all 
 
# Install tools used by the VSO build automation. 
RUN dnf install -y git \ 
        zip \ 
        tar \ 
        nodejs \ 
        npm && \ 
        findutils && \ 
    dnf clean all && \ 
    npm install -g azure-cli && \ 
    npm cache clean 
 
# Dependencies of CoreCLR and CoreFX. 
RUN dnf install -y libicu-devel \ 
        libuuid-devel \ 
        libcurl-devel \ 
        openssl-devel \ 
        libunwind-devel \ 
        lttng-ust-devel && \ 
    dnf clean all 
 
# Setup User to match Host User, and give superuser permissions 
ARG USER_ID=0 
RUN useradd -m code_executor -u ${USER_ID} -g wheel
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers 
 
# With the User Change, we need to change permissions on these directories 
RUN chmod -R a+rwx /usr/local 
RUN chmod -R a+rwx /home
 
# Set user to the one we just created 
USER ${USER_ID} 
 
# Set working directory 
WORKDIR /opt/code