# Testing Libraries on Android

To build the tests and run them on Android (devices or emulators) you need the following prerequisites.

- [Android NDK](https://developer.android.com/ndk/downloads)
- [Android SDK](https://developer.android.com/studio)
- OpenJDK

(TODO: provide command-line steps to download them with dependencies)

Once SDKs are downloaded, set `ANDROID_NDK_ROOT`(CLARIFY: looks like `ANDROID_NDK_HOME` should also be set) and `ANDROID_SDK_ROOT`.
Example:
```
export ANDROID_SDK_ROOT=/Users/egorbo/Library/Android/sdk
export ANDROID_NDK_ROOT=/Users/egorbo/android-ndk-r21b
export ANDROID_NDK_HOME=$ANDROID_NDK_ROOT
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
**IMPORTANT:** make sure correct ABIs are used in the path, e.g. `-arch x64` -> `android.x86_64` (TODO: auto-detect)

Now we're ready to build everything for Android:
```
./build.sh mono+libs -os Android -arch x64
```
and even run tests one by one for each test suite:
```
./build.sh libs.tests -os Android -arch x64 -test
```
Make sure an emulator is booted or a device is plugged and unlocked.
**NOTE**: Xharness doesn't run any UI on Android and runs tests using headless testing API so the device/emulator won't show anything (but still must stay active).

### Running individual test suites
- The following shows how to run tests for a specific library
```
cd src/libraries/System.Numerics.Vectors/tests
../../../.././dotnet.sh build /t:Test /p:TargetOS=Android /p:TargetArchitecture=x64
```

### Obtaining the logs
XHarness doesn't talk much and only saves tests result at the end to a file but you can also subscribe to live logs via the following command:
```
adb logcat -S DOTNET
```
Or simply open `logcat` window in Android Studio or Visual Stuido.

### Known Issues
- We don't support `-os Android` on Windows yet (`WSL` can be used instead)
