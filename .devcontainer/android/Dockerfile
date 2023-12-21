ARG VARIANT="8.0-jammy"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

# Set up machine requirements to build the repo
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        cmake \
        llvm \
        clang \
        build-essential \
        python3 \
        curl \
        git \
        lldb \
        liblldb-dev \
        libunwind8 \
        libunwind8-dev \
        gettext \
        libicu-dev \
        liblttng-ust-dev \
        libssl-dev \
        libkrb5-dev \
        zlib1g-dev \
        ninja-build \
        openjdk-17-jdk \
        pulseaudio

SHELL ["/bin/bash", "-c"]

ENV NDK_VER=r23c
ENV SDK_VER=9123335_latest
ENV SDK_API_LEVEL=33
ENV SDK_BUILD_TOOLS=33.0.1
ENV HOST_OS=linux
ENV HOST_OS_SHORT=linux
ENV ANDROID_NDK_ROOT=/android/android-ndk-${NDK_VER}
ENV ANDROID_SDK_ROOT=/android/android-sdk
ENV EMULATOR_NAME_X64=dotnet-android-emulator

# Download Android NDK and SDK
RUN curl -sSL --tlsv1.2 https://dl.google.com/android/repository/android-ndk-${NDK_VER}-${HOST_OS}.zip -L --output /tmp/andk.zip && \
    curl -sSL --tlsv1.2 https://dl.google.com/android/repository/commandlinetools-${HOST_OS_SHORT}-${SDK_VER}.zip -L --output /tmp/asdk.zip

# Check hashes of downloads
RUN (echo "6ce94604b77d28113ecd588d425363624a5228d9662450c48d2e4053f8039242 /tmp/andk.zip"; echo "0bebf59339eaa534f4217f8aa0972d14dc49e7207be225511073c661ae01da0a /tmp/asdk.zip") | cat | sha256sum -c

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
