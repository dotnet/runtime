# Using Native AOT with Android Bionic

Starting with .NET 8 Preview 7, it's possible to build shared libraries and command line executables for Android.

Not a full Android experience is available - it's only possible to publish for two Bionic RID: linux-bionic-arm64 and linux-bionic-x64. Publishing for Android RIDs (android-arm64/android-x64) is not possible. This limited experience corresponds to building with [Android NDK](https://developer.android.com/ndk) from Native code - the limitations are similar. Interop with Java needs to be done manually through JNI, if necessary.

The minimum API level is 21 at the time of writing the document, but search for AndroidApiLevelMin in this repo for more up-to-date information.

To build for Bionic:

* Ensure you have [Android NDK](https://developer.android.com/ndk/downloads) for your system downloaded and extracted somewhere. We build and test with NDK r23c but anything newer should also work. Newer releases of the NDK might require a workaround (#92272). Double check with the NDK version referenced [here](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing-android.md), which might be more up-to-date than this document.
* Update your PATH to include `{NDK_ROOT}\toolchains\llvm\prebuilt\{OS_ARCH}\bin`. Replace `{NDK_ROOT}` with the path where you extracted the NDK, and replace `{OS_ARCH}` with your host OS and architecture (e.g. windows-x86_64 or linux-x86_64). Make sure this entry is first in your path - we need to make sure that running `clang` will execute `clang` from this directory.
* You can either create a new project or use an existing project. For this guide, let's create a new one:
  ```sh
  $ dotnet new console -o HelloBionic --aot
  $ cd HelloBionic
  ```
  Note: the `--aot` parameter added `<PublishAot>true</PublishAot>` to the new project.
* Publish the project:
  ```sh
  $ dotnet publish -r linux-bionic-arm64 -p:DisableUnsupportedError=true -p:PublishAotUsingRuntimePack=true
  ```
* You should have a binary under `bin\Release\net8.0\linux-bionic-arm64\publish`. Copy it to an Android device. Either `adb push` or using some GUI.
* You can probably run it with `adb shell`, but I used Termux: open Termux, give it access to file system by running `termux-setup-storage`. This will give you access to phone storage under `~/storage`. Copy the binary from `~/storage/...` to `~` (internal storage is not executable and you won't be able to run stuff from it). Then `chmod +x HelloBionic` and `./HelloBionic`. You should see Hello World.

Command line apps are not very interesting for Android. The more interesting scenario are shared libraries that can be called into from Java/Kotlin through JNI. This is very similar to building shared libraries in other languages like C/C++/Rust. `PublishAot` allows building shared libraries that are callable from non-.NET languages. See https://learn.microsoft.com/dotnet/core/deploying/native-aot/interop#native-exports.

For an example of a Native AOT shared library invoked through JNI from Java see https://github.com/josephmoresena/NativeAOT-AndroidHelloJniLib.

## Libssl dependency

Crypto in .NET is implemented on top of OS-provided crypto libraries (we do not build or service crypto algorithm implementations). Since Android doesn't come with the standard openssl library inbox, your app will need to provide it. The runtime code can handle various versions of the openssl library. The library has to be placed next to the app.
