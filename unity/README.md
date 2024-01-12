# .NET Runtime - Unity Details

This is Unity's fork of the .NET Runtime repository.

The difference between this fork and the upstream repository should be as small as possible, with all of the differences specific to Unity. Our goal is to upstream as many changes made here as possible.


## Environment requirements

Please refer to [this page](https://github.com/dotnet/runtime/tree/main/docs/workflow/requirements) for environment requirements before build.



## Building, testing, and debugging locally

The CoreCLR runtime and class libraries can be built and tested locally for Windows, macOS, and Linux.

### Building the CoreCLR runtime and class libraries

To build locally, use the platform-specific shell script in .yamato/scripts. Either `build_yamato.cmd` or `build_yamato.sh`:
_Note that this will require having ninja installed, which is an optional requirement._

To view all possible arguments run:
```
> .yamato\scripts\build_yamato.cmd --h
```

Note: The defaults shown are specific to the system it is being run on.

```
> .yamato\scripts\build_yamato.cmd --arch x64 --config Debug
```

This will build the CoreCLR runtime and the class libraries for Windows, x64 architecture in the debug configuration. If you do not supply `arch` or `config` arguments the build will default to the current system architecture and Release. It also builds all targets by default if neither `--build` or `--test` arguments are supplied.

### Testing locally

To test locally, use the platform-specific script in .yamato/scripts:

```
> .yamato\scripts\build_yamato.cmd --arch x64 --config Debug --test
```

This will use the built artifacts from the build command above (Windows OS, x64 architecture, debug configuration) to run both Unity-specific tests and some tests we care about from CoreCLR.

### Building and testing together

The following will build all targets and then test all available targets

```
> .yamato\scripts\build_yamato.cmd --test --build
```

### Building or Testing a subset of avilable targets

It is possible to provide a space separated list of desired targets to build and/or test to `build_yamato.cmd`'s `--test` and `--build` arguments like so:

```
> .yamato\scripts\build_yamato.cmd --build NullGC EmbeddingHost --test EmbeddingManaged
```

### Using a debug build of the CoreCLR runtime in Unity

Once the CoreCLR runtime has been built locally you can find the runtime artifacts the Unity player needs in `artifacts\bin\microsoft.netcore.app.runtime.win-x64\Debug\runtimes\win-x64`. 

If you would like to patch a built player with your local build you can use
```
yamato\scripts\build_yamato.cmd --deploy-to-player --config Debug <Unity player build directory>
```

Notes

`--deploy-to-player` assumes the same default values as when using `--build`.

You can `--build` in conjuction with `--deploy-to-player` to build and copy in a single command

**Caveat:** It is _not_ possible to enable mixed mode debugging in Visual Studio and also debug the native code in coreclr.dll. Visual Studio blocks this workflow to prevent hangs. To debug in coreclr.dll in a Unity player, Native debugging must be selected in Visual Studio.

## Pulling changes from upstream

There is a job in Unity's internal CI which runs weekly to pull the latest code from the upstream [dotnet/runtime](https://github.com/dotnet/runtime) repository `main` branch and create a pull request to merge these changes to the [`unity-main`](https://github.com/Unity-Technologies/runtime/tree/unity-main) branch.

## Pushing changes to upstream

When a pull request is open against this fork, we should determine if the changes in that pull request should be pushed upstream (most should). Ideally, pull request should be organized so that all changes in a given pull request can be directly applied upstream. Any changes specific to the Unity fork should be done in a separate pull request.

Assuming the branch with changes to upstream is named `great-new-feature` then a new branch of upstream [`main`](https://github.com/dotnet/runtime/tree/main) named `upstream-great-new-feature` should be created. Each commit from `great-new-feature` should be cherry-picked `upstream-great-new-feature`, and then a pull request should be opened from `upstream-great-new-feature` to [`main`](https://github.com/dotnet/runtime/tree/main) in the upstream repository.

It is acceptable to _merge_ changes to this fork from `great-new-feature` before `upstream-great-new-feature` is merged, but we should at least _open_ an upstream pull request first.
