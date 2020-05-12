# Testing Libraries on Android

To build the tests and run them on Android (devices or emulators) you need the following prerequisites.

- [Android NDK](https://developer.android.com/ndk/downloads)
- [Android SDK](https://developer.android.com/studio)
- OpenJDK

(TODO: provide command-line steps to download them with dependencies)

Once SDKs are downloaded, set `ANDROID_NDK_ROOT` and `ANDROID_SDK_ROOT`.
Example:
```
export ANDROID_SDK_ROOT=/Users/egorbo/Library/Android/sdk
export ANDROID_NDK_ROOT=/Users/egorbo/android-ndk-r21b
```

Next, we need OpenSSL binaries and headers, we haven't properly integrated this dependency yet (it's an ongoing discussion) but there is a workaround:

- Download and unzip https://maven.google.com/com/android/ndk/thirdparty/openssl/1.1.1g-alpha-1/openssl-1.1.1g-alpha-1.aar
- Set these env variables:
```
GOOGLE_OPENSSL=/Users/egorbo/prj/openssl-1.1.1g-alpha-1.aar/prefab/modules
export AndroidOpenSslHeaders="$GOOGLE_OPENSSL/ssl/include"
export AndroidOpenSslCryptoLib="$GOOGLE_OPENSSL/crypto/libs/android.x86_64/libcrypto.so"
export AndroidOpenSslLib="$GOOGLE_OPENSSL/ssl/libs/android.x86_64/libssl.so"
```
**IMPORTANT:** make sure correct ABIs are used in the path, e.g. `-arch x64` -> `android.x86_64`

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
runner called XHarness TestRunner which runs tests for all *.Tests.dll libs in the bundle. There is also XHarness.CLI tool with ADB inside to deploy `*.apk` to a target (device or emulator) and obtain logs once tests are completed.

### Obtaining the logs
XHarness for Android doesn't talk much and only saves tests result to a file once tests finished but you can also subscribe to live logs via the following command:
```
adb logcat -s "DOTNET"
```
Or simply open `logcat` window in Android Studio or Visual Stuido.

### Known Issues
- We don't support `-os Android` on Windows yet (`WSL` can be used instead)
