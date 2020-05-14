# Testing Libraries on Android

To build the tests and run them on Android (devices or emulators) you need the following prerequisites.

- Android NDK
- Android SDK
- OpenJDK 8

## Linux
The following script should install those dependencies and add `ANDROID_SDK_ROOT`, `ANDROID_NDK_ROOT` and `ANDROID_OPENSSL_AAR`
environment variables to the `~/.bashrc`:
```bash
#!/usr/bin/env bash

NDK_VER=r21b
SDK_VER=6200805_latest
SDK_API_LEVEL=29
SDK_BUILD_TOOLS=29.0.3
BASHRC=~/.bashrc
OPENSSL_VER=1.1.1g-alpha-1

export ANDROID_NDK_ROOT=~/android-ndk-${NDK_VER}
export ANDROID_SDK_ROOT=~/android-sdk
export ANDROID_OPENSSL_AAR=~/openssl-android

curl https://dl.google.com/android/repository/android-ndk-${NDK_VER}-linux-x86_64.zip -L --output ~/andk.zip
unzip ~/andk.zip -d $(dirname ${ANDROID_NDK_ROOT}) && rm -rf ~/andk.zip

curl https://dl.google.com/android/repository/commandlinetools-linux-${SDK_VER}.zip -L --output ~/asdk.zip
unzip ~/asdk.zip -d ${ANDROID_SDK_ROOT} && rm -rf ~/asdk.zip
yes | ${ANDROID_SDK_ROOT}/tools/bin/./sdkmanager --sdk_root=${ANDROID_SDK_ROOT} --licenses
${ANDROID_SDK_ROOT}/tools/bin/./sdkmanager --sdk_root=${ANDROID_SDK_ROOT} "platform-tools" "platforms;android-${SDK_API_LEVEL}" "build-tools;${SDK_BUILD_TOOLS}"

# We also need to download precompiled binaries and headers for OpenSSL from maven, this step is a temporary hack
# and will be removed once we figure out how to integrate OpenSSL properly as a dependency
curl https://maven.google.com/com/android/ndk/thirdparty/openssl/${OPENSSL_VER}/openssl-${OPENSSL_VER}.aar -L --output ~/openssl.zip
unzip ~/openssl.zip -d ${ANDROID_OPENSSL_AAR} && rm -rf ~/openssl.zip

printf "\n\nexport ANDROID_NDK_ROOT=${ANDROID_NDK_ROOT}\nexport ANDROID_SDK_ROOT=${ANDROID_SDK_ROOT}\nexport ANDROID_OPENSSL_AAR=${ANDROID_OPENSSL_AAR}\n" >> ${BASHRC}
```

## macOS

*TODO:* 

## Building Libs and Tests for Android

Now we're ready to build everything for Android:
```
./build.sh mono+libs -os Android -arch x64
```
and even run tests one by one for each test suite:
```
./build.sh libs.tests -os Android -arch x64 -test
```
Make sure an emulator is booted (see `AVD Manager`) or a device is plugged in and unlocked.

**NOTE**: Xharness doesn't run any UI on Android and runs tests using headless testing API so the device/emulator won't show anything (but still must stay active).

### Running individual test suites
- The following shows how to run tests for a specific library
```
./dotnet.sh build /t:Test src/libraries/System.Numerics.Vectors/tests /p:TargetOS=Android /p:TargetArchitecture=x64
```

### How the tests work
Android app is basically a [Java Instrumentation](https://github.com/dotnet/runtime/blob/master/src/mono/msbuild/AndroidAppBuilder/Templates/MonoRunner.java) and a simple Activity that inits the Mono Runtime via JNI. This Mono Runtime starts a simple xunit test
runner called XHarness TestRunner which runs tests for all `*.Tests.dll` libs in the bundle. There is also XHarness.CLI tool with ADB inside to deploy `*.apk` to a target (device or emulator) and obtain logs once tests are completed.

### Obtaining the logs
XHarness for Android doesn't talk much and only saves tests result to a file once tests finished but you can also subscribe to live logs via the following command:
```
adb logcat -s "DOTNET"
```
Or simply open `logcat` window in Android Studio or Visual Stuido.

### Known Issues
- We don't support `-os Android` on Windows yet (`WSL` can be used instead)
