# Testing Libraries on Android

The following dependencies should be installed in order to be able to run tests:

- Android NDK
- Android SDK
- OpenJDK
- OpenSSL

OpenJDK can be installed on Linux (Ubuntu) using `apt-get`:
```bash
sudo apt-get install openjdk-8 unzip
```

Android SDK, NDK and OpenSSL can be automatically installed via the following script:
```bash
#!/usr/bin/env bash
set -e

NDK_VER=r21b
SDK_VER=6200805_latest
SDK_API_LEVEL=29
SDK_BUILD_TOOLS=29.0.3
OPENSSL_VER=1.1.1g-alpha-1

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
curl https://dl.google.com/android/repository/android-ndk-${NDK_VER}-${HOST_OS}-x86_64.zip -L --output ~/andk.zip
unzip ~/andk.zip -d $(dirname ${ANDROID_NDK_ROOT}) && rm -rf ~/andk.zip

# download Android SDK, accept licenses and download additional packages such as
# platform-tools, platforms and build-tools
export ANDROID_SDK_ROOT=~/android-sdk
curl https://dl.google.com/android/repository/commandlinetools-${HOST_OS_SHORT}-${SDK_VER}.zip -L --output ~/asdk.zip
mkdir ${ANDROID_SDK_ROOT} && unzip ~/asdk.zip -d ${ANDROID_SDK_ROOT}/cmdline-tools && rm -rf ~/asdk.zip
yes | ${ANDROID_SDK_ROOT}/cmdline-tools/tools/bin/sdkmanager --sdk_root=${ANDROID_SDK_ROOT} --licenses
${ANDROID_SDK_ROOT}/cmdline-tools/tools/bin/sdkmanager --sdk_root=${ANDROID_SDK_ROOT} "platform-tools" "platforms;android-${SDK_API_LEVEL}" "build-tools;${SDK_BUILD_TOOLS}"
```

## Building Libs and Tests for Android

Now we're ready to build everything for Android:
```
./build.sh mono+libs -os Android -arch x64
```
and even run tests one by one for each library:
```
./build.sh libs.tests -os Android -arch x64 -test
```
Make sure an emulator is booted (see [`AVD Manager`](#avd-manager)) or a device is plugged in and unlocked.
`AVD Manager` tool recommends to install `x86` images by default so if you follow that recommendation make sure `-arch x86` was used for the build script.

### Running individual test suites
The following shows how to run tests for a specific library
```
./dotnet.sh build /t:Test src/libraries/System.Numerics.Vectors/tests /p:TargetOS=Android /p:TargetArchitecture=x64
```

### Test App Design
Android app is basically a [Java Instrumentation](https://github.com/dotnet/runtime/blob/main/src/mono/msbuild/AndroidAppBuilder/Templates/MonoRunner.java) and a simple Activity that inits the Mono Runtime via JNI. This Mono Runtime starts a simple xunit test
runner called XHarness.TestRunner (see https://github.com/dotnet/xharness) which runs tests for all `*.Tests.dll` libs in the bundle. There is also XHarness.CLI tool with ADB embedded to deploy `*.apk` to a target (device or emulator) and obtain logs once tests are completed.

### Obtaining the logs
XHarness for Android doesn't talk much and only saves test results to a file. However, you can also subscribe to live logs via the following command:
```
adb logcat -s "DOTNET"
```
Or simply open `logcat` window in Android Studio or Visual Stuido.

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
- `-os Android` is not supported for Windows yet (`WSL` can be used instead)
- XHarness.CLI is not able to boot emulators yet (so you need to boot via `AVD Manager` or IDE)
- AOT and Interpreter modes are not supported yet
