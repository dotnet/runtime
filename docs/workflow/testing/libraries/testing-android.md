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
unzip ~/asdk.zip -d ${ANDROID_SDK_ROOT} && rm -rf ~/asdk.zip
yes | ${ANDROID_SDK_ROOT}/tools/bin/./sdkmanager --sdk_root=${ANDROID_SDK_ROOT} --licenses
${ANDROID_SDK_ROOT}/tools/bin/./sdkmanager --sdk_root=${ANDROID_SDK_ROOT} "platform-tools" "platforms;android-${SDK_API_LEVEL}" "build-tools;${SDK_BUILD_TOOLS}"

# We also need to download precompiled binaries and headers for OpenSSL from maven, this step is a temporary hack
# and will be removed once we figure out how to integrate OpenSSL properly as a dependency
export ANDROID_OPENSSL_AAR=~/openssl-android
curl https://maven.google.com/com/android/ndk/thirdparty/openssl/${OPENSSL_VER}/openssl-${OPENSSL_VER}.aar -L --output ~/openssl.zip
unzip ~/openssl.zip -d ${ANDROID_OPENSSL_AAR} && rm -rf ~/openssl.zip
printf "\n\nexport ANDROID_NDK_ROOT=${ANDROID_NDK_ROOT}\nexport ANDROID_SDK_ROOT=${ANDROID_SDK_ROOT}\nexport ANDROID_OPENSSL_AAR=${ANDROID_OPENSSL_AAR}\n" >> ${BASHRC}
```
Save it to a file (e.g. `deps.sh`) and execute using `source` (e.g. `chmod +x deps.sh && source ./deps.sh`) in order to propogate the `ANDROID_NDK_ROOT`, `ANDROID_SDK_ROOT` and `ANDROID_OPENSSL_AAR` environment variables to the current process.

## Building Libs and Tests for Android

Now we're ready to build everything for Android:
```
./build.sh mono+libs -os Android -arch x64
```
and even run tests one by one for each library:
```
./build.sh libs.tests -os Android -arch x64 -test
```
Make sure an emulator is booted (see `AVD Manager`) or a device is plugged in and unlocked.
`AVD Manager` tool recommends to install `x86` images by default so if you follow that recommendation make sure `-arch x86` was used for the build script.

### Running individual test suites
The following shows how to run tests for a specific library
```
./dotnet.sh build /t:Test src/libraries/System.Numerics.Vectors/tests /p:TargetOS=Android /p:TargetArchitecture=x64
```

### Test App Design
Android app is basically a [Java Instrumentation](https://github.com/dotnet/runtime/blob/master/src/mono/msbuild/AndroidAppBuilder/Templates/MonoRunner.java) and a simple Activity that inits the Mono Runtime via JNI. This Mono Runtime starts a simple xunit test
runner called XHarness.TestRunner (see https://github.com/dotnet/xharness) which runs tests for all `*.Tests.dll` libs in the bundle. There is also XHarness.CLI tool with ADB embedded to deploy `*.apk` to a target (device or emulator) and obtain logs once tests are completed.

### Obtaining the logs
XHarness for Android doesn't talk much and only saves test results to a file. However, you can also subscribe to live logs via the following command:
```
adb logcat -s "DOTNET"
```
Or simply open `logcat` window in Android Studio or Visual Stuido.

### Existing Limitations
- `-os Android` is not supported for Windows yet (`WSL` can be used instead)
- XHarness.CLI is not able to boot emulators yet (so you need to boot via `AVD Manager` or IDE)
- AOT and Interpreter modes are not supported yet