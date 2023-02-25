# NativeAOT iOS sample app

## Description

This sample application is intended to be used by developers who work on enabling NativeAOT on iOS-like platforms and can serve as PoC for verifying support for the following systems:
- ios
- iossimulator
- tvos
- tvossimulator
- maccatalyst

The sample shares the source code with the Mono sample specified at: `../iOS/Program.cs` and in general should have the same behavior as MonoAOT.

## Limitations

The application is **_currently_** relying on the following:
1. Internal dependencies - locally building the internals is required as runtime and tools nuget packages are still not being produced
2. Invariant globalization - `System.Globalization.Native` is currently not being built as part of NativeAOT framework for iOS-like platforms
3. No publish targets - the SDK and MSBuild integration is still not complete

## How to build and test

### Building for the first time

When building for the first time (on a clean checkout) run from this directory the following `make` command:
``` bash
make world
```
This will first build all required runtime components and dependencies, after which it will build the sample app and bundle it into an application bundle.
By default the build will use `Debug` build configuration and target `iossimulator`.
To change this behavior, specify the desired setting in the following way:
``` bash
make world BUILD_CONFIG=Release TARGET_OS=ios
```

### To avoid building all the dependencies

For future builds, you can run just:
``` bash
make
```
which will skip building all the runtime dependencies, assuming those have been already properly built, and build the MSBuild task used for bundling the application and the application it self.

For convenience, it is also possible to rebuild only the application it self with:
``` bash
make hello-app
```

### Deploy and run

#### Simulator

To test the application on a simulator include the following in your make command `DEPLOY_AND_RUN=true` e.g.,:
``` bash
make hello-app DEPLOY_AND_RUN=true
```

#### Device

To test the application on a device, a provisioning profile needs to be specified.
This can be achieved by defining `DevTeamProvisioning` environment variable with a valid team ID (see [developer.apple.com/account/#/membership](https://developer.apple.com/account/#/membership), scroll down to `Team ID`) for example:
``` bash
export DevTeamProvisioning=A1B2C3D4E5; make hello-app TARGET_OS=ios DEPLOY_AND_RUN=true
```
Assuming `A1B2C3D4E5` is a valid team ID.

#### One-liner

On a clean dotnet/runtime checkout, from this directory, run:

``` bash
export DevTeamProvisioning=A1B2C3D4E5; make world BUILD_CONFIG=Release TARGET_OS=ios DEPLOY_AND_RUN=true
```

This command will build everything necessary to run and deploy the application on an iOS device.

### Custom builds

Check the `Makefile` for individual list of targets and variables to customize the build.
