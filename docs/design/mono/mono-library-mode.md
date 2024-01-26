Library Mode on Mono
===

# Background

For many native applications, accessibility to bountiful APIs from .NET runtime libraries can save developers from "reinventing the wheel" in the target platform's native language. That is where [interoperability](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/interop/) comes in handy to access modern .NET APIs from the native side. The .NET runtime libraries require the .NET runtime to function properly, and integrating the entire .NET ecosystem may prove cumbersome and unnecessary. Instead, for a smaller footprint and more seamless experience, the runtime and custom managed code invoking .NET APIs can be bundled into a library for direct consumption. In line with [Native code interop with Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/interop), as of .NET 8, the [mono runtime supports a library mode](https://github.com/dotnet/runtime/issues/79377) enabling mobile developers to leverage modern .NET APIs in their mobile applications with a single static or shared library.

Note: The library generated from Mono's Library Mode containing custom managed code and the mono runtime will, for brevity, be referred to as the mono library.

# How it works

The core components of mono's library mode that enables interoperability between native and managed code are as follows:
1. [UnmanagedCallersOnlyAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.unmanagedcallersonlyattribute?view=net-7.0) which allows native code to directly call managed methods.
2. [Direct Platform Invoke (P/Invoke)](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke) which allows managed code to directly call native functions.
3. The mono runtime which facilitates the above interop directions among its other responsibilities as a [managed runtime](https://learn.microsoft.com/en-us/dotnet/core/introduction#runtime).

Being able to call managed .NET APIs from a native application has many usecases, including reducing the need to rewrite logic in the native language when there is no native counterpart. In order to call managed code leveraging these .NET APIs, the native application needs to recognize the corresponding symbols. Once custom managed code is compiled into managed assemblies, the Mono AOT Compiler processes them to generate [native-to-managed wrappers](https://github.com/dotnet/runtime/blob/43d164d8d65d163fef0de185eb11cfa0b1291919/src/mono/mono/mini/aot-compiler.c#L5446-L5498) for all methods decorated with [`UnmanagedCallersOnlyAttribute`](https://github.com/dotnet/runtime/pull/79424). These native-to-managed wrappers have entrypoint symbols specified by the corresponding `UnmanagedCallersOnlyAttribute`, allowing native code to call them directly. So once the mono library is linked/loaded into the native application, the Mono AOT Compiled assemblies should be [preloaded by the mono runtime](https://github.com/dotnet/runtime/blob/43d164d8d65d163fef0de185eb11cfa0b1291919/src/tasks/LibraryBuilder/Templates/preloaded-assemblies.c#L10) once the mono runtime is initialized in order to enable calling managed methods from the native side of the application.

Being able to call native (unmanaged) functions from managed code is equally as important for bridging the native and managed sides. It can grant the managed side access to system-level operations and facilitates the reuse of native libraries where there are no managed counterparts. In order to call native code from managed code, the entrypoints to the native functions need to be known by the managed side. The mono runtime leverages managed-to-native wrappers to perform Direct P/Invoke by using these entrypoints to direct the native runtime to execute the corresponding native function. The Mono AOT Compiler [generates managed-to-native wrappers](https://github.com/dotnet/runtime/blob/9a33ac520a67496c8f79139dc571867726dc0e45/src/mono/mono/mini/aot-compiler.c#L5288-L5317) for p/invokes methods that are specified to be directly callable, which can be done either [altogether, by module names, or even by exactly matching module name and entrypoint name](https://github.com/dotnet/runtime/pull/79721).

Interoperability is contingent on having a managed runtime, which in this case is the mono runtime. Though the mono runtime is [linked into the mono library](https://github.com/dotnet/runtime/blob/df6fdefa27068126794b253d4d822706221a92db/src/tasks/LibraryBuilder/LibraryBuilder.cs#L338), it needs to be running in order for interoperability to occur. By design, the mono runtime is initialized once the native application calls into the mono library, through invoking a native-to-managed wrapper's entrypoint symbol. A [`runtime-init-callback` must be set](https://github.com/dotnet/runtime/pull/82253) either manually or automatically, so that the first native-to-managed wrapper called can invoke mono's runtime init function.

## Auto-initializing the Mono Runtime

Auto-initialization of the mono runtime, as mentioned in [How it works](#how-it-works), occurs when using the default runtime init callback [`UsesRuntimeInitCallback=true`](https://github.com/dotnet/runtime/blob/df6fdefa27068126794b253d4d822706221a92db/src/tasks/LibraryBuilder/LibraryBuilder.cs#L81) and [`UsesCustomRuntimeInitCallback=false`](https://github.com/dotnet/runtime/blob/df6fdefa27068126794b253d4d822706221a92db/src/tasks/LibraryBuilder/LibraryBuilder.cs#L76) instead of a custom callback. It [involves several steps](https://github.com/dotnet/runtime/blob/df6fdefa27068126794b253d4d822706221a92db/src/tasks/LibraryBuilder/Templates/autoinit.c#L125-L161) to setup the mono runtime for proper behavior. Once the native application calls into the mono library through a native-to-managed wrapper entry point, the callback is invoked once in a thread safe manner. In cases where the default callback isn't appropriate, a custom callback may be set using `UsesRuntimeInitCallback=true` + `UsesCustomRuntimeInitCallback=true` + [`CustomRuntimeInitCallback=<custom callback>`](https://github.com/dotnet/runtime/blob/df6fdefa27068126794b253d4d822706221a92db/src/mono/msbuild/apple/build/AppleBuild.targets#L169C100-L169C125), and it is the implementor's responsibility to design a thread safe implementation of lazy runtime initialization.

## Bundling

As the mono library provides native applications a means to access .NET APIs, the resources corresponding to those APIs such as assemblies, their pdbs containing debugging and symbol information, satellite assemblies for localization, and other data resources like runtime configuration and timezone data need to be accessible as well. This can be achieved through having those resources on disk, but for a more out-of-the-box solution, the mono library can be [built as a self-contained library](https://github.com/dotnet/runtime/pull/84191) by bundling needed resources into the library itself. In doing so, the byte data of needed resources are stored in preallocated structs in the library that [should then be registered into the mono runtime during initialization](https://github.com/dotnet/runtime/blob/76a995afe3306863cb836b5becc33293a2e5a781/src/tasks/LibraryBuilder/Templates/autoinit.c#L130).

# Example Workflows

## Building from a dotnet sdk workload

https://github.com/steveisok/library-mode-sample

Note: The workload might be named differently depending on the sdk version, e.g. `mobile-librarybuilder`. Search for available workloads using `dotnet workload search` and passing in keywords like `mobile` or `librarybuilder`.

## Android

After building the mono library with `dotnet publish -r android-arm64`, it can be found as `lib<Managed Project Name>.so` in the binaries folder (i.e. `library-mode-sample/ManagedProject/bin/Release/net8.0/android-arm64/Bundle/libManagedProject.so`). The mono library when built as a shared library with bundling (on by default) can be loaded and used with the following steps:

1. Open/Create the Android native project in Android Studio.

2. Copy the mono library into the project's `jniLibs` folder under the corresponding architecture (create directories if necessary). i.e. `app/src/main/jniLibs/arm64-v8a/libManagedProject.so`

3. Load the mono library through Java Native Interface by creating a C++ module under `app/src/main/cpp/`. If the C++ module option is not available, create a `.cpp` file and a `CMakeLists.txt` file that should contain the following.

C++
```cpp
#include <jni.h>

extern "C" void SayHello();

extern "C"
{
    JNIEXPORT void JNICALL
    Java_com_example_<package name>_MainActivity_SayHello(JNIEnv *env, jobject thiz) {
        SayHello();
    }
}
```

CMake
```
cmake_minimum_required(VERSION 3.22.1)

project("<your project>")

add_library( android_library_mode
             SHARED
             <filename>.cpp )

find_library( log-lib
              log )

target_link_libraries( android_library_mode
                       ${log-lib}
                       ${CMAKE_SOURCE_DIR}/../jniLibs/arm64-v8a/libManagedProject.so)
```

4. Instantiate and call the Create a `Copy Files` build phase with `Frameworks` as the destination, and include the mono library.

5. Load the library that links in the mono library (i.e. `android_library_mode` from the `.cpp` file) via `System.loadLibrary("<your library>")`.

6. Instantiate the native methods and invoke them.

MainActivity.java
```java
public class MainActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        System.loadLibrary("android_library_mode");

        SayHello();
    }

    public native void SayHello();
}
```

7. Building and running the Android application should reflect the additions.

## iOS

After building the mono library with `dotnet publish -r ios-arm64`, it can be found as `lib<Managed Project Name>.dylib` in the binaries folder (i.e. `library-mode-sample/ManagedProject/bin/Release/net8.0/ios-arm64/Bundle/libManagedProject.dylib`). The mono library when built as a shared library with bundling (on by default) can be loaded and used with the following steps:

1. Open/Create the iOS native project in XCode.

2. Copy the mono library into the project's root directory (not creating a reference).

3. Navigate to the project's `Build Phases` tab, and ensure that the mono library is included under the `Link Binary With Libraries` section.

4. Create a `Copy Files` build phase with `Frameworks` as the destination, and include the mono library.

5. Navigate to the project's `Build Settings` tab, and add the directory where the mono library was placed in step 2. to `Library Search Paths`. i.e. `$(PROJECT_DIR)` if the mono library was placed in the root directory.

6. Instantiate and call any custom managed code built into the mono library in native code. i.e. Add `void SayHello(void);` to `main.m` and invoke it `SayHello();`.

7. Building and running the iOS application should reflect the additions.