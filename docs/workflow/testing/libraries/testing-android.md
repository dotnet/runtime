# Testing Libraries on Android using Mono runtime

> [!NOTE]
> This document covers testing with the Mono runtime on Android. For testing with CoreCLR on Android, see [CoreCLR Android Documentation](../../building/coreclr/android.md).

## Table of Contents

- [Prerequisites](#prerequisites)
  - [Using a terminal](#using-a-terminal)
  - [Using Android Studio](#using-android-studio)
- [Building Libs and Tests for Android](#building-libs-and-tests-for-android)
  - [Running individual test suites](#running-individual-test-suites)
  - [Running the functional tests](#running-the-functional-tests)
  - [Testing various configurations](#testing-various-configurations)
  - [Test App Design](#test-app-design)
  - [Obtaining the logs](#obtaining-the-logs)
  - [AVD Manager](#avd-manager)
  - [Existing Limitations](#existing-limitations)
  - [Debugging the native runtime code using Android Studio](#debugging-the-native-runtime-code-using-android-studio)
- [Upgrading the Android NDK Version in CI Pipelines](#upgrading-the-android-ndk-version-in-ci-pipelines)
  - [1. Verify the New NDK Version Locally](#1-verify-the-new-ndk-version-locally)
  - [2. Test the New NDK in CI and Fix Issues](#2-test-the-new-ndk-in-ci-and-fix-issues)
  - [3. Update the NDK Version in the Prerequisites Repository](#3-update-the-ndk-version-in-the-prerequisites-repository)

## Prerequisites

The following dependencies should be installed in order to be able to run tests:

- OpenJDK
- Android NDK
- Android SDK

To manage the dependencies, you can install them via terminal or using Android Studio.

### Using a terminal

OpenJDK can be installed on Linux (Ubuntu) using `apt-get`:
```bash
sudo apt-get install openjdk-8-jdk zip unzip
```

Android SDK and NDK can be automatically installed via the following script:
```bash
#!/usr/bin/env bash
set -e

NDK_VER=r27c
ANDROID_CLI_TOOLS_VER=13114758_latest
SDK_API_LEVEL=36
SDK_BUILD_TOOLS=36.0.0

if [[ "$OSTYPE" == "darwin"* ]]; then
    HOST_OS=darwin
    HOST_OS_SHORT=mac
    BASHRC=~/.zprofile
else
    HOST_OS=linux
    HOST_OS_SHORT=linux
    BASHRC=~/.bashrc
fi

# download Android NDK
export ANDROID_NDK_ROOT=~/android-ndk-${NDK_VER}
curl https://dl.google.com/android/repository/android-ndk-${NDK_VER}-${HOST_OS}.zip -L --output ~/andk.zip
unzip ~/andk.zip -d $(dirname ${ANDROID_NDK_ROOT}) && rm -rf ~/andk.zip

# download Android SDK, accept licenses and download additional packages such as
# platform-tools, platforms and build-tools
export ANDROID_SDK_ROOT=~/android-sdk
curl https://dl.google.com/android/repository/commandlinetools-${HOST_OS_SHORT}-${ANDROID_CLI_TOOLS_VER}.zip -L --output ~/asdk.zip
mkdir ${ANDROID_SDK_ROOT} && unzip ~/asdk.zip -d ${ANDROID_SDK_ROOT}/cmdline-tools && rm -rf ~/asdk.zip
yes | ${ANDROID_SDK_ROOT}/cmdline-tools/cmdline-tools/bin/sdkmanager --sdk_root=${ANDROID_SDK_ROOT} --licenses
${ANDROID_SDK_ROOT}/cmdline-tools/cmdline-tools/bin/sdkmanager --sdk_root=${ANDROID_SDK_ROOT} "platform-tools" "platforms;android-${SDK_API_LEVEL}" "build-tools;${SDK_BUILD_TOOLS}"
```

### Using Android Studio

Android Studio offers a convenient UI:
- to install all the dependencies;
- to manage android virtual devices;
- to make easy use of adb logs.

## Building Libs and Tests for Android

Before running a build you might want to set the Android SDK and NDK environment variables:
```
export ANDROID_SDK_ROOT=<PATH-TO-ANDROID-SDK>
export ANDROID_NDK_ROOT=<PATH-TO-ANDROID-NDK>
```

Now we're ready to build everything for Android:
```
./build.sh mono+libs -os android -arch x64
```
and even run tests one by one for each library:
```
./build.sh libs.tests -os android -arch x64 -test
```
Make sure an emulator is booted (see [`AVD Manager`](#avd-manager)) or a device is plugged in and unlocked.
`AVD Manager` tool recommends to install `x86` images by default so if you follow that recommendation make sure `-arch x86` was used for the build script.

### Running individual test suites
The following shows how to run tests for a specific library
```
./dotnet.sh build /t:Test src/libraries/System.Numerics.Vectors/tests /p:TargetOS=android /p:TargetArchitecture=x64 /p:RuntimeFlavor=mono
```

### Running the functional tests

There are [functional tests](https://github.com/dotnet/runtime/tree/main/src/tests/FunctionalTests/) which aim to test some specific features/configurations/modes on a target mobile platform.

A functional test can be run the same way as any library test suite, e.g.:
```
./dotnet.sh build /t:Test -c Release /p:TargetOS=android /p:TargetArchitecture=x64 /p:RuntimeFlavor=mono src/tests/FunctionalTests/Android/Device_Emulator/PInvoke/Android.Device_Emulator.PInvoke.Test.csproj
```

Currently functional tests are expected to return `42` as a success code so please be careful when adding a new one.

### Testing various configurations

It's possible to test various configurations by setting a combination of additional MSBuild properties such as `RunAOTCompilation`,`MonoForceInterpreter`, and some more.

1. AOT

To build for AOT only mode, add `/p:RunAOTCompilation=true /p:MonoForceInterpreter=false` to a build command.

2. AOT-LLVM

To build for AOT-LLVM mode, add `/p:RunAOTCompilation=true /p:MonoForceInterpreter=false /p:MonoEnableLLVM=true` to a build command.

3. Interpreter

To build for Interpreter mode, add `/p:RunAOTCompilation=false /p:MonoForceInterpreter=true` to a build command.

### Test App Design
Android app is basically a [Java Instrumentation](https://github.com/dotnet/runtime/blob/main/src/tasks/AndroidAppBuilder/Templates/MonoRunner.java) and a simple Activity that inits the Mono Runtime via JNI. This Mono Runtime starts a simple xunit test
runner called XHarness.TestRunner (see https://github.com/dotnet/xharness) which runs tests for all `*.Tests.dll` libs in the bundle. There is also XHarness.CLI tool with ADB embedded to deploy `*.apk` to a target (device or emulator) and obtain logs once tests are completed.

### Obtaining the logs
XHarness for Android doesn't talk much and only saves test results to a file. However, you can also subscribe to live logs via the following command:
```
adb logcat -s "DOTNET"
```
Or simply open `logcat` window in Android Studio or Visual Studio.

### AVD Manager
If Android Studio is installed, [AVD Manager](https://developer.android.com/studio/run/managing-avds) can be used from the IDE to create and start Android virtual devices. Otherwise, the Android SDK provides the [`avdmanager` command line tool](https://developer.android.com/studio/command-line/avdmanager).

Example of installing, creating, and launching emulators from the command line (where `SDK_API_LEVEL` matches the installed Android SDK and `EMULATOR_NAME_X86`/`EMULATOR_NAME_X64` are names of your choice):
```bash
# Install x86 image
${ANDROID_SDK_ROOT}/cmdline-tools/tools/bin/sdkmanager "system-images;android-${SDK_API_LEVEL};default;x86"

# Create x86 image
${ANDROID_SDK_ROOT}/cmdline-tools/tools/bin/avdmanager create avd --name ${EMULATOR_NAME_X86} --package "system-images;android-${SDK_API_LEVEL};default;x86"

# Launch emulator with x86 image
${ANDROID_SDK_ROOT}/emulator/emulator -avd ${EMULATOR_NAME_X86} &

# Install x64 image
${ANDROID_SDK_ROOT}/cmdline-tools/tools/bin/sdkmanager "system-images;android-${SDK_API_LEVEL};default;x86_64"

# Create x64 image
${ANDROID_SDK_ROOT}/cmdline-tools/tools/bin/avdmanager create avd --name ${EMULATOR_NAME_X64} --package "system-images;android-${SDK_API_LEVEL};default;x86_64"

# Launch emulator with x64 image
${ANDROID_SDK_ROOT}/emulator/emulator -avd ${EMULATOR_NAME_X64} &
```
The emulator can be launched with a variety of options. Run `emulator -help` to see the full list.

### Existing Limitations
- `-os android` is not supported for Windows yet (`WSL` can be used instead)
- XHarness.CLI is not able to boot emulators yet (so you need to boot via `AVD Manager` or IDE)
- AOT and Interpreter modes are not supported yet

### Debugging the native runtime code using Android Studio

See [Debugging Android](../../debugging/mono/android-debugging.md)

## Upgrading the Android NDK Version in CI Pipelines

The Android NDK has two release channels: a rolling release, which occurs approximately every quarter, and a Long Term Support (LTS) release, which happens once a year (typically in Q3). While release dates are not guaranteed, LTS versions receive support for at least one year or until the next LTS reaches the release candidate stage. After that, the NDK version stops receiving bug fixes and security updates.

The LTS NDK release schedule roughly aligns with the .NET Release Candidate (RC) timeline. Given this, we should plan to upgrade the NDK version used in `main` around that time. If we successfully upgrade before .NET release, we can ensure that our CI builds and tests run against a supported NDK version for approximately 9 months after the release.

.NET MAUI is supported for 18 months after each .NET release. This means the NDK version used in CI will be supported for about half the lifecycle of a given .NET MAUI release. If we want to ensure that the NDK version used in CI is supported for the entire lifecycle of a given .NET MAUI release, we should consider upgrading the NDK version in the `release` branches.

CI pipelines retrieve the NDK version from Docker images hosted in the [dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker) repository.

For reference, see an example Dockerfile NDK definition:
[Azure Linux 3.0 .NET 10.0 Android Dockerfile](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/c480b239b3731983e36b0879f5b60d8f4ab7b945/src/azurelinux/3.0/net10.0/android/amd64/Dockerfile#L2).

Bumping version of the NDK in the prereqs repo will automatically propagate it to all CI runs Thus, bumping the NDK requires a three step process in order to ensure that CI continues to operate correctly.
To upgrade the NDK version used in CI for building and testing Android, follow these steps:

### 1. Verify the New NDK Version Locally
- Download the new NDK version.
- Test the local build using the new NDK by building a sample Android app.
- Ensure **AOT** and **AOT_WITH_LIBRARY_FILES** are enabled in the build.

### 2. Test the New NDK in CI and Fix Issues
- Create a new Docker image containing the updated NDK version (based on the original docker image from the [dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker) repository).
- Open a **draft PR** in the **runtime** repository that updates the Dockerfile reference to use the new image.
- Monitor CI results and fix any failures.
- Once CI is green, **commit only the necessary changes** (e.g., fixes, build adjustments) to the respective branch.
- **Do not** change the Docker image reference in the final commit.

### 3. Update the NDK Version in the Prerequisites Repository
- Update the NDK version in the [dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker) repository by modifying the Dockerfile.
- The updated NDK will automatically flow to all builds of a given branch once merged.

By following these steps, you ensure a smooth upgrade of the Android NDK in CI while maintaining stability and compatibility.
