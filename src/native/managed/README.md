# Native runtime component libraries using NativeAOT

This directory contains managed libraries that will be compiled using NativeAOT and can be used in runtime components.

## Using existing libraries

In a `CMakeLists.txt` add

``` cmake
find_nativeaot_library(libWhatever REQUIRED)
```

This will look for a cmake fragment file in
`artifacts/obj/cmake/find_package/libWhatever-config.cmake` that will import some variables from
`artifacts/obj/libWhatever/libWhatever.cmake` and defines a new `libWhatever::libWhatever` that can
be used as a dependency in `target_link_libraries()` or in a `install_clr()` command.


## Adding a new managed library

Add a new subdirectory to `src/native/managed` for your library with a `src` and `inc` subdirectories:

``` console
$ mkdir -p libMyNewLibrary/src libMyNewLibrary/inc
$ dotnet new classlib -n libMyNewLibrary -o libMyNewLibrary/src
```

In `src/native/managed/compile-native.proj`, add
`src/native/managed/libMyNewLibrary/src/libMyNewLibrary.csproj` to the `NativeLibsProjectsToBuild`
item group.

In `src/native/managed/libMyNewLibrary/src/libMyNewLibrary.csproj`:
1. Near the top,  add `<Import Project="..\..\native-library.props" />`
2. Near the bottom, add `<Import Project="..\..\native-library.targets" />`

The following is recommended:

1. The project should be called `libXXXX` - currently the infrastructure expects a `lib` prefix on all platforms.
2. The project should just have a single `EntryPoints.cs` that has a `static class` that provides
   `[UnmanagedCallersOnly]` API entrypoints.  The bulk of the code should be imported using
   `ProjectReference` from another managed class library project.  That managed project can be
   tested using normal managed unit tests, consumed in other managed libraries, etc.
3. The `inc` directory if it exists will be added to the include path of any cmake targets that
   depend on the native library. Typically the directory should contain a `libMyNewLibrary.h` header
   file with a C declaration for each entrypoint.  If you want ot have more include directories, add
   them all to the `NativeLibraryCmakeFragmentIncludePath` item.  In that case `inc` won't be added
   by default.

Limitations:

Currently only shared library output is supported.  In principle static linking is possible, but the
infrastructure is not finished yet.  Additionally, mixing Debug/Release configurations with static
linking will not be supported on Windows.
