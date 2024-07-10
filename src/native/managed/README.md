# Native runtime component libraries using NativeAOT

This directory contains managed libraries that will be compiled using NativeAOT and can be used in runtime components.

## Adding a new managed library

Add a new subdirectory to `src/native/managed` for your library with a `src`, `inc` and `test` subdirectories:

``` console
$ mkdir -p libMyNewLibrary/src libMyNewLibrary/inc libMyNewLibrary/test
$ dotnet new classlib -n libMyNewLibrary -o libMyNewLibrary/src
```

In `src/native/managed/compile-native.proj`, add
`src/native/managed/libMyNewLibrary/src/libMyNewLibrary.csproj` to the `NativeLibsProjectsToBuild`
item group.

In `src/native/managed/libMyNewLibrary/src/libMyNewLibrary.csproj`:
1. Define an item `@(InstallRuntimeComponentDestination)` that has directory names relative to `artifacts/bin/<runtimeFlavor>/<os.arch.config>/` where the shared library should be installed.  It's a good idea to have at least `.`:
    ```xml
      <ItemGroup>
          <InstallRuntimeComponentDestination Include="." />
          <InstallRuntimeComponentDestination Include="sharedFramework" Condition="'$(RuntimeFlavor)' == 'coreclr'"/>
      </ItemGroup>
    ```

Limitations:

* The project should be called `libXXXX` - currently the infrastructure expects a `lib` prefix on all platforms.

* Currently only shared library output is supported.  In principle static linking is possible, but the
infrastructure is not finished yet.  Additionally, mixing Debug/Release configurations with static
linking will not be supported on Windows.
