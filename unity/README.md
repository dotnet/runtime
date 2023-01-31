# .NET Runtime - Unity Details

This is Unity's fork of the .NET Runtime repository.

The difference between this fork and the upstream repository should be as small as possible, with all of the differences specific to Unity. Our goal is to upstream as many changes made here as possible.


## Environment requirements

Please refer to [this page](https://github.com/dotnet/runtime/tree/main/docs/workflow/requirements) for environment requirements before build.



## Building, testing, and debugging locally

The CoreCLR runtime and class libraries can be built and tested locally for Windows, macOS, and Linux.

### Building the CoreCLR runtime and class libraries

To build locally, use the platform-specific script in .yamato/scripts:

```
> .yamato\scripts\build_windows.cmd x64 Debug
```

This will build the CoreCLR runtime and the class libraries for Windows, x64 architecture in the debug configuration.

### Testing locally

To test locally, use the platform-specific script in .yamato/scripts:

```
.yamato\scripts\test_windows.cmd x64 Debug
```

This will use the built artifacts from the build command above (Windows OS, x64 architecture, debug configuration) to run both Unity-specific tests and some tests we care about from CoreCLR.

### Building and testing together

Use the scripts in the unity/test-scripts directory to run the build and tests together:

```
unity/test-scripts/build_test_windows.cmd x64 Debug
```

### Using a debug build of the CoreCLR runtime in Unity

Once the CoreCLR runtime has been built locally you can find the runtime artifacts the Unity player needs in `artifacts\bin\microsoft.netcore.app.runtime.win-x64\Debug\runtimes\win-x64`. Replace `Debug` with `Release` in this path for the Release configuration. It is important to copy all of the runtime and class library files together, as they need to stay in sync in order for the CoreCLR runtime to work properly. On Windows, the recursive directory copy (overwriting files in the destination) looks like this:

Windows:
```
> xcopy /e /y artifacts\bin\microsoft.netcore.app.runtime.win-x64\Debug\runtimes\win-x64 <Unity player build directory>\CoreCLR
```

macOS:
```
> cp -r artifacts/bin/microsoft.netcore.app.runtime.osx-<your arch>/Debug/runtimes/osx-<your arch>/* <Unity player build directory>/Contents/Resources/Data/CoreCLR
```

**Caveat:** It is _not_ possible to enable mixed mode debugging in Visual Studio and also debug the native code in coreclr.dll. Visual Studio blocks this workflow to prevent hangs. To debug in coreclr.dll in a Unity player, Native debugging must be selected in Visual Studio.

## Pulling changes from upstream

There is a job in Unity's internal CI which runs weekly to pull the latest code from the upstream [dotnet/runtime](https://github.com/dotnet/runtime) repository `main` branch and create a pull request to merge these changes to the [`unity-main`](https://github.com/Unity-Technologies/runtime/tree/unity-main) branch.

## Pushing changes to upstream

When a pull request is open against this fork, we should determine if the changes in that pull request should be pushed upstream (most should). Ideally, pull request should be organized so that all changes in a given pull request can be directly applied upstream. Any changes specific to the Unity fork should be done in a separate pull request.

Assuming the branch with changes to upstream is named `great-new-feature` then a new branch of upstream [`main`](https://github.com/dotnet/runtime/tree/main) named `upstream-great-new-feature` should be created. Each commit from `great-new-feature` should be cherry-picked `upstream-great-new-feature`, and then a pull request should be opened from `upstream-great-new-feature` to [`main`](https://github.com/dotnet/runtime/tree/main) in the upstream repository.

It is acceptable to _merge_ changes to this fork from `great-new-feature` before `upstream-great-new-feature` is merged, but we should at least _open_ an upstream pull request first.
