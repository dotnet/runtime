ARG VARIANT="8.0-noble"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

# Set up machine requirements to build the repo
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        clang \
        cmake \
        cpio \
        build-essential \
        python3 \
        curl \
        git \
        lldb \
        llvm \
        liblldb-dev \
        libunwind8 \
        libunwind8-dev \
        gettext \
        libicu-dev \
        liblttng-ust-dev \
        libssl-dev \
        libkrb5-dev \
        ninja-build \
        ninja-build \
        openjdk-17-jdk \
        pulseaudio

SHELL ["/bin/bash", "-c"]

ENV NDK_VER=r27c
ENV SDK_VER=13114758_latest
ENV SDK_API_LEVEL=35
ENV SDK_BUILD_TOOLS=35.0.0
ENV HOST_OS=linux
ENV HOST_OS_SHORT=linux
ENV ANDROID_NDK_ROOT=/android/android-ndk-${NDK_VER}
ENV ANDROID_SDK_ROOT=/android/android-sdk
ENV EMULATOR_NAME_X64=dotnet-android-emulator

# Download Android NDK and SDK
RUN curl -sSL --tlsv1.2 https://dl.google.com/android/repository/android-ndk-${NDK_VER}-${HOST_OS}.zip -L --output /tmp/andk.zip && \
    curl -sSL --tlsv1.2 https://dl.google.com/android/repository/commandlinetools-${HOST_OS_SHORT}-${SDK_VER}.zip -L --output /tmp/asdk.zip

# Check hashes of downloads
RUN (echo "59c2f6dc96743b5daf5d1626684640b20a6bd2b1d85b13156b90333741bad5cc /tmp/andk.zip"; echo "7ec965280a073311c339e571cd5de778b9975026cfcbe79f2b1cdcb1e15317ee /tmp/asdk.zip") | cat | sha256sum -c

# Unpack the NDK and SDK
RUN mkdir -p ${ANDROID_NDK_ROOT} && unzip /tmp/andk.zip -d $(dirname ${ANDROID_NDK_ROOT}) && rm -f /tmp/andk.zip && \
    mkdir -p ${ANDROID_SDK_ROOT} && unzip /tmp/asdk.zip -d ${ANDROID_SDK_ROOT}/cmdline-tools && rm -f /tmp/asdk.zip

# Setup the SDK
RUN yes | ${ANDROID_SDK_ROOT}/cmdline-tools/cmdline-tools/bin/sdkmanager --sdk_root=${ANDROID_SDK_ROOT} --licenses && \
    ${ANDROID_SDK_ROOT}/cmdline-tools/cmdline-tools/bin/sdkmanager --sdk_root=${ANDROID_SDK_ROOT} "platform-tools" "platforms;android-${SDK_API_LEVEL}" "build-tools;${SDK_BUILD_TOOLS}"

# Install an x86_64 emulator
RUN ${ANDROID_SDK_ROOT}/cmdline-tools/cmdline-tools/bin/sdkmanager --sdk_root=${ANDROID_SDK_ROOT} "system-images;android-${SDK_API_LEVEL};default;x86_64"

# Setup kvm group. We need the group ID to be GID owner of /dev/kvm.
# Then add the vscode user to the kvm group.
RUN groupadd -r -g 109 kvm && \
    gpasswd -a vscode kvm
