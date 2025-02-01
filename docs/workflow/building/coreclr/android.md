# Experimental support of CoreCLR on Android

This is the internal documentation which outlines experimental support of CoreCLR on Android and includes instructions on how to:
- [Build CoreCLR for Android](./android.md#building-coreclr-for-android)
- [Build and run a sample Android application with CoreCLR](./android.md#building-and-running-helloandroid-sample-app)
- [Debug the sample app and the runtime](./android.md#debugging-the-sample-app)

## Prerequisite

Download and install `Android Studio` and the following:
  - Android SDK (minimum supported API level is 21)
  - Android NDK r27

## Building CoreCLR for Android

Supported host systems for building CoreCLR for Android:
- [MacOS](./android.md#macos) ✔
- [Linux](./android.md#linux) ✔
- [Windows](./android.md#windows) ❌ (only through WSL)

Supported target architectures:
- x86 ❌
- x64 ✔
- arm ❌
- arm64 ✔

### MacOS

#### Requirements

Set the following environment variables:
  - ANDROID_SDK_ROOT=`<full-path-to-android-sdk>`
  - ANDROID_NDK_ROOT=`<full-path-to-android-ndk>`

For example:
  ```
  ANDROID_SDK_ROOT=/Users/<user>/Library/Android/sdk
  ANDROID_NDK_ROOT=/Users/<user>/Library/Android/sdk/ndk/27.2.12479018
  ```

#### Building the runtime, libraries and tools

To build CoreCLR runtime packages, libraries and tools, run the following command from `<repo-root>`:

```
./build.sh clr.runtime+clr.alljits+clr.corelib+clr.nativecorelib+clr.tools+clr.packages+libs+packs -os android -arch <x64|arm64> -c <Debug|Release> -cross -bl
```

NOTE: The runtime packages will be located at: `<repo-root>/artifacts/packages/<configuration>/Shipping/`

### Linux

TODO: Bellow are the old notes which we need to reevaluate

Through cross compilation, on Linux it is possible to build CoreCLR for arm64 Android.

#### Requirements

You'll need to generate a toolchain and a sysroot for Android. There's a script which takes care of the required steps.

#### Generating the rootfs

To generate the rootfs, run the following command in the `coreclr` folder:

```
cross/init-android-rootfs.sh
```

This will download the NDK and any packages required to compile Android on your system. It's over 1 GB of data, so it may take a while.


#### Cross compiling CoreCLR

Once the rootfs has been generated, it will be possible to cross compile CoreCLR.

When cross compiling, you need to set both the `CONFIG_DIR` and `ROOTFS_DIR` variables.

To compile for arm64, run:

```
CONFIG_DIR=`realpath cross/android/arm64` ROOTFS_DIR=`realpath cross/android-rootfs/toolchain/arm64/sysroot` ./build.sh cross arm64 cmakeargs -DENABLE_LLDBPLUGIN=0
```

The resulting binaries will be found in `artifacts/bin/coreclr/Linux.BuildArch.BuildType/`

#### Running the PAL tests on Android

You can run the PAL tests on an Android device. To run the tests, you first copy the PAL tests to your Android phone using
`adb`, and then run them in an interactive Android shell using `adb shell`:

To copy the PAL tests over to an Android phone:
```
adb push artifacts/obj/coreclr/Linux.arm64.Debug/src/pal/tests/palsuite/ /data/local/tmp/coreclr/pal/tests/palsuite
adb push cross/android/toolchain/arm64/sysroot/usr/lib/libandroid-support.so /data/local/tmp/coreclr/lib/
adb push cross/android/toolchain/arm64/sysroot/usr/lib/libandroid-glob.so /data/local/tmp/coreclr/lib/
adb push src/pal/tests/palsuite/paltestlist.txt /data/local/tmp/coreclr
adb push src/pal/tests/palsuite/runpaltests.sh /data/local/tmp/coreclr/
```

Then, use `adb shell` to launch a shell on Android. Inside that shell, you can launch the PAL tests:
```
LD_LIBRARY_PATH=/data/local/tmp/coreclr/lib ./runpaltests.sh /data/local/tmp/coreclr/
```

#### Debugging coreclr on Android

You can debug coreclr on Android using a remote lldb server which you run on your Android device.

First, push the lldb server to Android:

```
adb push cross/android/lldb/2.2/android/arm64-v8a/lldb-server /data/local/tmp/
```

Then, launch the lldb server on the Android device. Open a shell using `adb shell` and run:

```
adb shell
cd /data/local/tmp
./lldb-server platform --listen *:1234
```

After that, you'll need to forward port 1234 from your Android device to your PC:
```
adb forward tcp:1234 tcp:1234
```

Finally, install lldb on your PC and connect to the debug server running on your Android device:

```
lldb-3.9
(lldb) platform select remote-android
  Platform: remote-android
 Connected: no
(lldb) platform connect connect://localhost:1234
  Platform: remote-android
    Triple: aarch64-*-linux-android
OS Version: 23.0.0 (3.10.84-perf-gf38969a)
    Kernel: #1 SMP PREEMPT Fri Sep 16 11:29:29 2016
  Hostname: localhost
 Connected: yes
WorkingDir: /data/local/tmp

(lldb) target create coreclr/pal/tests/palsuite/file_io/CopyFileA/test4/paltest_copyfilea_test4
(lldb) env LD_LIBRARY_PATH=/data/local/tmp/coreclr/lib
(lldb) run
```

### Windows

Building on Windows is not directly supported yet. However it is possible to use WSL2 for this purpose.

#### WSL2

##### Requirements

1. Install the Android SDK and NDK in WSL per the [prerequisites](#prerequisite). This can be done by downloading the archives or using Android Studio:
    - Archives:
      - [NDK](https://developer.android.com/ndk/downloads) and [command-line tools](https://developer.android.com/studio#command-line-tools-only) (use `sdkmanager` to download the SDK)
      - For an automated script, see in [Testing Libraries on Android](../../testing/libraries/testing-android.md#using-a-terminal)
    - Android Studio:
      - Make sure WSL is updated: from Windows host, `wsl --update`
      - [Enabled systemd](https://devblogs.microsoft.com/commandline/systemd-support-is-now-available-in-wsl/#set-the-systemd-flag-set-in-your-wsl-distro-settings)
      - `sudo snap install android-studio --classic`
2. Set the following environment variables:
    - ANDROID_SDK_ROOT=`<full-path-to-android-sdk>`
    - ANDROID_NDK_ROOT=`<full-path-to-android-ndk>`

#### Building the runtime, libraries and tools

To build CoreCLR runtime, libraries and tools, run the following command from `<repo-root>`:

```
./build.sh clr.runtime+clr.alljits+clr.corelib+clr.nativecorelib+clr.tools+clr.packages+libs+packs -os android -arch <x64|arm64> -c <Debug|Release>
```

## Building and running HelloAndroid sample app

To demonstrate building and running an Android sample application with CoreCLR, we will use the [HelloAndroid sample app](../../../../src/mono/sample/Android/AndroidSampleApp.csproj).
A prerequisite of this step is to have CoreCLR successfully built for desired Android platform.

### Building HelloAndroid sample

To build `HelloAndroid`, run the following command from `<repo_root>`:

```
make BUILD_CONFIG=<Debug|Release> TARGET_ARCH=<x64|arm64> RUNTIME_FLAVOR=CoreCLR DEPLOY_AND_RUN=false run -C src/mono/sample/Android
```

On successful execution, the command will output the `HelloAndroid.apk` at:
```
<repo-root>artifacts/bin/AndroidSampleApp/arm64/Release/android-arm64/Bundle/bin/HelloAndroid.apk
```

### Running HelloAndroid sample on an emulator

To run the sample on an emulator, the emulator first needs to be up and running.

Creating an emulator (ADV - Android Virtual Device) can be achieved through Android Studio - Device Manager: https://developer.android.com/studio/run/managing-avds

After its creation, the emulator needs to be booted up and running, so that we can run the `HelloAndroid` sample on it via:
```
make BUILD_CONFIG=<Debug|Release> TARGET_ARCH=<x64|arm64> RUNTIME_FLAVOR=CoreCLR DEPLOY_AND_RUN=true run -C src/mono/sample/Android
```

NOTE: Emulators can be also started from the terminal via:
```
$ANDROID_SDK_ROOT/emulator/emulator -avd <emulator-name>
```

#### WSL2

The app can be run on an emulator running on the Windows host.
1. Install Android Studio on the Windows host (same versions as in [prerequisites](#prerequisite))
2. In Windows, create and start an emulator
3. In WSL, swap the `adb` from the Android SDK in WSL2 with that from Windows
    - `mv $ANDROID_SDK_ROOT/platform-tools/adb $ANDROID_SDK_ROOT/platform-tools/adb-orig`
    - `ln -s /mnt/<path-to-adb-on-host> $ANDROID_SDK_ROOT/platform-tools/adb`
4. In WSL, Make xharness use the `adb` corresponding to the Windows host:
    - `export ADB_EXE_PATH=$ANDROID_SDK_ROOT/platform-tools/adb`
5. In WSL, run the `make` command as [above](#running-helloandroid-sample-on-an-emulator)

### Useful make commands

For convenience it is possible to run a single make command which builds all required dependencies, the app and runs it:
```
make BUILD_CONFIG=<Debug|Release> TARGET_ARCH=<x64|arm64> RUNTIME_FLAVOR=CoreCLR DEPLOY_AND_RUN=true all -C src/mono/sample/Android
```

## Debugging the runtime and the sample app

Managed debugging is currently not supported, but we can debug:
- Java portion of the sample app
- Native code for the CoreCLR host and the runtime it self

This can be achieved in `Android Studio` via `Profile or Debug APK`.

### Steps

1. Build the runtime and `HelloAndroid` sample app in `Debug` configuration.
2. Rename the debug symbols file of the runtime library from `libcoreclr.so.dbg` into `libcoreclr.so.so`, the file is located at: `<repo_root>/artifacts/bin/AndroidSampleApp/arm64/Debug/android-arm64/publish/libcoreclr.so.dbg`
3. Open Android Studio and select `Profile or Debug APK` project.
4. Find and select the desired `.apk` file (example: `<repo_root>/artifacts/bin/AndroidSampleApp/arm64/Release/android-arm64/Bundle/bin/HelloAndroid.apk`)
5. In the project pane, expand `HelloAndroid->cpp->libcoreclr` and double-click `libcoreclr.so`
![Adding debug symbols](./android-studio-coreclr-debug-symbols-adding.png)
6. From the `Debug Symbols` pane on the right, select `Add`
7. Navigate to the renamed file from step 2. and select it `<repo_root>/artifacts/bin/AndroidSampleApp/arm64/Debug/android-arm64/publish/libcoreclr.so.so`
8. Once loaded it will show all the source files under `HelloAndroid->cpp->libcoreclr`
![Debug symbols loaded](./android-studio-coreclr-debug-symbols-adding.png)
9. Find the `exports.cpp` and set a breakpoint in `coreclr_initialize` function and launch the debug session
![Debugging CoreCLR](./android-studio-coreclr-debugging.png)

## See also

Similar instructions for debugging Android apps with Mono runtime can be found [here](../../debugging/mono/android-debugging.md).