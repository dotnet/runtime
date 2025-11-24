# Experimental support of CoreCLR on Android

This is the internal documentation which outlines experimental support of CoreCLR on Android.

## Table of Contents

- [Prerequisite](#prerequisite)
- [Building CoreCLR for Android](#building-coreclr-for-android)
  - [MacOS and Linux](#macos-and-linux)
    - [Requirements](#requirements)
    - [Building the runtime, libraries and tools](#building-the-runtime-libraries-and-tools)
  - [Windows + WSL2](#windows--wsl2)
    - [Windows requirements](#windows-requirements)
    - [WSL requirements](#wsl-requirements)
    - [Building the runtime, libraries and tools](#building-the-runtime-libraries-and-tools-1)
- [Building and running a sample app](#building-and-running-a-sample-app)
  - [Building HelloAndroid sample](#building-helloandroid-sample)
  - [Running HelloAndroid sample on an emulator](#running-helloandroid-sample-on-an-emulator)
    - [WSL2](#wsl2)
- [Building and running tests on an emulator](#building-and-running-tests-on-an-emulator)
- [Debugging the runtime and the sample app](#debugging-the-runtime-and-the-sample-app)
  - [Steps](#steps)
- [See also](#see-also)
- [Troubleshooting](#troubleshooting)
  - [Android samples or functional tests fail to build](#android-samples-or-functional-tests-fail-to-build)
    - [java.lang.NullPointerException: Cannot invoke String.length()](#javalangnullpointerexception-cannot-invoke-stringlength)

## Building CoreCLR for Android

Supported host systems for building CoreCLR for Android:
- [MacOS](./android.md#macos-and-linux) ✔
- [Linux](./android.md#macos-and-linux) ✔
- [Windows](./android.md#windows) ❌ (only through WSL)

Supported target architectures:
- x86 ❌
- x64 ✔
- arm ❌
- arm64 ✔

### MacOS and Linux

#### Prerequisites

- Download and install [OpenJDK 23](https://openjdk.org/projects/jdk/23/)
- Download and install [Android Studio](https://developer.android.com/studio/install) and the following:
  - Android SDK (minimum supported API level is 21)
  - Android NDK r27c

> [!NOTE]
> Prerequisites can also be downloaded and installed manually:
> - An automated script as described in [Testing Libraries on Android](../../testing/libraries/testing-android.md#using-a-terminal)
> - Downloading the archives:
>   - Android SDK - Download [command-line tools](https://developer.android.com/studio#command-line-tools-only) and use `sdkmanager` to download the SDK.
>   - Android NDK - Download [NDK](https://developer.android.com/ndk/downloads)

Set the following environment variables:
  - `export ANDROID_SDK_ROOT=<full-path-to-android-sdk>`
  - `export ANDROID_NDK_ROOT=<full-path-to-android-ndk>`

#### Building the runtime, libraries and tools

To build CoreCLR runtime, libraries and tools for local development, run the following command from `<repo-root>`:

```
./build.sh clr.runtime+clr.alljits+clr.corelib+clr.nativecorelib+clr.tools+clr.packages+libs -os android -arch <x64|arm64> -c <Debug|Release>
```

To build CoreCLR runtime NuGet packages, run the following command from `<repo-root>`:

```
./build.sh clr.runtime+clr.alljits+clr.corelib+clr.nativecorelib+clr.tools+clr.packages+libs+host+packs -os android -arch <x64|arm64> -c <Debug|Release>
```

> [!NOTE]
> The runtime packages will be located at: `<repo-root>/artifacts/packages/<configuration>/Shipping/`

> [!NOTE]
> The static CoreCLR runtime for static linking (`libcoreclr_static.a`) is available in inner build artifacts but is not yet shipped in NuGet packages.

### Windows + WSL2

Building on Windows is not directly supported yet. However it is possible to use WSL2 for this purpose.

#### Windows prerequisites

- Download and install [Android Studio](https://developer.android.com/studio/install)
- Enable [long paths](../../requirements/windows-requirements.md#enable-long-paths)

#### WSL prerequisites

1. Follow [linux-requirements.md](../../requirements/linux-requirements.md).
2. Install OpenJDK, Android SDK and Android NDK in as described in [Linux prerequisites](#prerequisites). There is a convenient automated script, but it can also be done manually by downloading the archives or using Android Studio.
- In case of Android Studio:
    - Make sure WSL is updated: from Windows host, `wsl --update`
    - [Enabled systemd](https://devblogs.microsoft.com/commandline/systemd-support-is-now-available-in-wsl/#set-the-systemd-flag-set-in-your-wsl-distro-settings)
    - `sudo snap install android-studio --classic`
- For Ubuntu, OpenJDK 21 is sufficient:

```
apt install openjdk-21-jdk
```

3. Set the following environment variables:
    - `export ANDROID_SDK_ROOT=<full-path-to-android-sdk>`
    - `export ANDROID_NDK_ROOT=<full-path-to-android-ndk>`

#### Building the runtime, libraries and tools

To build CoreCLR runtime, libraries and tools, run the following command from `<repo-root>`:

```
./build.sh clr.runtime+clr.alljits+clr.corelib+clr.nativecorelib+clr.tools+clr.packages+libs -os android -arch <x64|arm64> -c <Debug|Release>
```

## Building and running a sample app

To demonstrate building and running an Android application with CoreCLR, we will use the [HelloAndroid sample app](../../../../src/mono/sample/Android/AndroidSampleApp.csproj).

A prerequisite for building and running samples locally is to have CoreCLR successfully built for desired Android platform.

### Building HelloAndroid sample

To build `HelloAndroid`, run the following command from `<repo_root>`:

```
make BUILD_CONFIG=<Debug|Release> TARGET_ARCH=<x64|arm64> RUNTIME_FLAVOR=CoreCLR DEPLOY_AND_RUN=false run -C src/mono/sample/Android
```

On successful execution, the command will output the `HelloAndroid.apk` at:
```
<repo-root>artifacts/bin/AndroidSampleApp/<x64|arm64>/<Debug|Release>/android-<x64|arm64>/Bundle/bin/HelloAndroid.apk
```

### Running HelloAndroid sample on an emulator

To run the sample on an emulator, the emulator first needs to be up and running.

Creating an emulator (ADV - Android Virtual Device) can be achieved through [Android Studio - Device Manager](https://developer.android.com/studio/run/managing-avds).

After its creation, the emulator needs to be booted up and running, so that we can run the `HelloAndroid` sample on it via:
```
make BUILD_CONFIG=<Debug|Release> TARGET_ARCH=<x64|arm64> RUNTIME_FLAVOR=CoreCLR DEPLOY_AND_RUN=true run -C src/mono/sample/Android
```


> [!NOTE]
> Emulators can be also started from the terminal via:
> ```
> $ANDROID_SDK_ROOT/emulator/emulator -avd <emulator-name>
> ```

#### WSL2

The app can be run on an emulator running on the Windows host.
1. Install Android Studio on the Windows host (same versions as in [prerequisites](#prerequisite))
2. In Windows, create and start an emulator
3. In WSL, swap the `adb` from the Android SDK in WSL2 with that from Windows
    - `mv $ANDROID_SDK_ROOT/platform-tools/adb $ANDROID_SDK_ROOT/platform-tools/adb-orig`
    - `ln -s /mnt/<path-to-sdk-on-host>/platform-tools/adb.exe $ANDROID_SDK_ROOT/platform-tools/adb`
    - On Windows host, you can find the SDK location in Android Studio's SDK Manager.
4. In WSL, Make xharness use the `adb` corresponding to the Windows host:
    - `export ADB_EXE_PATH=$ANDROID_SDK_ROOT/platform-tools/adb`
5. In WSL, run the `make` command as [above](#running-helloandroid-sample-on-an-emulator)

## Building and running tests on an emulator

To demonstrate building and running tests on CoreCLR Android, we will use the [Android.Device_Emulator.JIT.Test](../../../../src/tests/FunctionalTests/Android/Device_Emulator/JIT/Android.Device_Emulator.JIT.Test.csproj) test project.

To build and run the test on Android with CoreCLR, run the following command from `<repo_root>`:

```
./dotnet.sh build -c <Debug|Release> src/tests/FunctionalTests/Android/Device_Emulator/JIT/Android.Device_Emulator.JIT.Test.csproj /p:TargetOS=android /p:TargetArchitecture=<x64|arm64> /t:Test /p:RuntimeFlavor=coreclr
```

> [!NOTE]
> Similarly to the `HelloAndroid` sample, the emulator needs to be up and running.

## Debugging the runtime and the sample app

Managed debugging is currently not supported, but we can debug:
- Java portion of the sample app
- Native code for the CoreCLR host and the runtime it self

This can be achieved in `Android Studio` via `Profile or Debug APK`.

### Steps

1. Build the runtime and `HelloAndroid` sample app in `Debug` configuration targeting `arm64` target architecture.
2. Rename the debug symbols file of the runtime library from `libcoreclr.so.dbg` into `libcoreclr.so.so`, the file is located at: `<repo_root>/artifacts/bin/AndroidSampleApp/arm64/Debug/android-arm64/publish/libcoreclr.so.dbg`
3. Open Android Studio and select `Profile or Debug APK` project.
4. Find and select the desired `.apk` file (example: `<repo_root>/artifacts/bin/AndroidSampleApp/arm64/Debug/android-arm64/Bundle/bin/HelloAndroid.apk`)
5. In the project pane, expand `HelloAndroid->cpp->libcoreclr` and double-click `libcoreclr.so`
![Adding debug symbols](./android-studio-coreclr-debug-symbols-adding.png)
6. From the `Debug Symbols` pane on the right, select `Add`
7. Navigate to the renamed file from step 2. and select it `<repo_root>/artifacts/bin/AndroidSampleApp/arm64/Debug/android-arm64/publish/libcoreclr.so.so`
8. Once loaded it will show all the source files under `HelloAndroid->cpp->libcoreclr`
![Debug symbols loaded](./android-studio-coreclr-debug-symbols-adding.png)
9. Find the `exports.cpp` and set a breakpoint in `coreclr_initialize` function and launch the debug session
![Debugging CoreCLR](./android-studio-coreclr-debugging.png)

> [!NOTE]
> Steps 5) through 8) can be omitted if the runtime is built without stripping debug symbols to a separate file (e.g., `libcoreclr.so.dbg`).
> This can be achieved by including `-keepnativesymbols true` option when building the runtime, e.g.,:
> ```
> ./build.sh clr.runtime+clr.alljits+clr.corelib+clr.nativecorelib+clr.tools+clr.packages+libs -os android -arch <x64|arm64> -c Debug -keepnativesymbols true
> ```

## See also

Similar instructions for debugging Android apps with Mono runtime can be found [here](../../debugging/mono/android-debugging.md).

## Troubleshooting

### Android samples or functional tests fail to build

#### java.lang.NullPointerException: Cannot invoke String.length()

If multiple JDKs are installed on your system, you may encounter the following error:

```
`src/mono/msbuild/android/build/AndroidBuild.targets(237,5): error MSB4018: java.lang.NullPointerException: Cannot invoke String.length() because <parameter1> is null
```

when building the Android samples or functional tests.

To resolve this:
1. Remove older JDK versions
2. Install [OpenJDK 23](https://openjdk.org/projects/jdk/23/)
3. Make sure OpenJDK 23 binaries are added to the path.
  - On Unix system this can be verifed via:
  ```
  $> java -version
  openjdk version "23.0.1" 2024-10-15
  OpenJDK Runtime Environment Homebrew (build 23.0.1)
  OpenJDK 64-Bit Server VM Homebrew (build 23.0.1, mixed mode, sharing)
  ```
