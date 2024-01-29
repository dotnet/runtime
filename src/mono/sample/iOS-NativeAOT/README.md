# NativeAOT iOS sample app

## Description

This sample application is intended to be used by developers who work on enabling NativeAOT on iOS-like platforms and can serve as PoC for verifying support for the following systems:
- ios
- iossimulator
- tvos
- tvossimulator
- maccatalyst

The sample shares the source code with the Mono sample specified at: `../iOS/Program.cs` and in general should have the same behavior as MonoAOT.

## Scenarios

There are two scenarios for building and testing the NativeAOT support for iOS:
1. Testing against locally built internals
    - Uses locally built ILCompiler, build integration targets, framework and runtime libraries
    - Should be used in CI testing
2. Testing against locally built packages - **end-to-end** testing
    - References locally built ILCompiler and runtime packages
    - Should be used by developers performing end-to-end validation

## How to build and test

### 1. Testing against locally built internals

When building for the first time (on a clean checkout) run from this directory the following `make` command:
``` bash
make world
```

The command performs the following:
1. Build all required runtime components and dependencies
2. Build the sample app and bundle it into an application bundle

By default the build will use `Debug` build configuration and target `iossimulator`.
To change this behavior, specify the desired setting in the following way:
``` bash
make world BUILD_CONFIG=Release TARGET_OS=ios
```

### 2. Testing against locally built packages - end-to-end testing

When building for the first time (on a clean checkout) run from this directory the following `make` command:
``` bash
make world USE_RUNTIME_PACKS=true
```

The command performs the following:
1. Builds ILCompiler and runtime packages:
    - `Microsoft.DotNet.ILCompiler.8.0.0-dev` (host)
    - `runtime.<host_os>-<host_arch>.Microsoft.DotNet.ILCompiler.8.0.0-dev` (host)
    - `Microsoft.NETCore.App.Runtime.NativeAOT.<target_os>-<target_arch>.8.0.0-dev` (target)

    NOTE:
    - The packages can be found at: `artifacts/packages/<config>/Shipping/*.8.0.0-dev.nupkg`
    - During the build of the application NuGet is instructed to use the local `./packages` folder (in the current directory) as the folder for restored packages, which should be deleted when testing incremental changes. This is required as the packages built locally always have the same version - `8.0.0-dev`. For convenience, targets in the `Makefile` are automatically removing this folder on a rebuild.
2. Build the sample app using locally built packages 1) and bundle it into an application bundle

By default the build will use `Debug` build configuration and target `iossimulator`.
To change this behavior, specify the desired setting in the following way:
``` bash
make world USE_RUNTIME_PACKS=true BUILD_CONFIG=Release TARGET_OS=ios
```

NOTE: In general, the make variable `USE_RUNTIME_PACKS` controls which scenario will be used during the build (the default value is `false`)

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

NOTE: Pay attention to the scenario you are testing `USE_RUNTIME_PACKS=true or false`

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

### One-liners

On a clean dotnet/runtime checkout, from this directory, run:

``` bash
export DevTeamProvisioning=A1B2C3D4E5; make world BUILD_CONFIG=Release TARGET_OS=ios DEPLOY_AND_RUN=true
```

- This command will build everything necessary to run and deploy the application on an iOS device using the locally built internals.

``` bash
export DevTeamProvisioning=A1B2C3D4E5; make world BUILD_CONFIG=Release TARGET_OS=ios DEPLOY_AND_RUN=true USE_RUNTIME_PACKS=true
```

- This command will build everything necessary to run and deploy the application on an iOS device using the locally built packages.

### Custom builds

Check the `Makefile` for individual list of targets and variables to further customize your builds.
