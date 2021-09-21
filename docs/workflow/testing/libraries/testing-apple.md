# Testing Libraries on iOS, tvOS, and MacCatalyst

## Prerequisites

- XCode 11.3 or higher

## Building Libs and Tests

You can build and run the library tests:
- on a simulator;
- on a device.

Run the following command in a terminal:
```
./build.sh mono+libs -os <TARGET_OS> -arch x64
```
where `<TARGET_OS>` is one of the following:

| simulator     | device|
|:--------------|:------|
| iOSSimulator  | iOS   |
| tvOSSimulator | tvOS  |
| MacCatalyst   |       |


e.g., to build for a iOS simulator, run:
```
./build.sh mono+libs -os iOSSimulator -arch x64
```

Run tests one by one for each test suite on a simulator:
```
./build.sh libs.tests -os iOSSimulator -arch x64 -test
```

### Building for a device

In order to run the tests on a device:
- Set the `-os` parameter to a device-related value (see above);
- Specify `DevTeamProvisioning` (see [developer.apple.com/account/#/membership](https://developer.apple.com/account/#/membership), scroll down to `Team ID`), e.g.:
```
./build.sh libs.tests -os iOS -arch x64 -test /p:DevTeamProvisioning=H1A2B3C4D5
```
[AppleAppBuilder](https://github.com/dotnet/runtime/blob/main/src/mono/msbuild/AppleAppBuilder/AppleAppBuilder.cs) generates temp Xcode projects you can manually open and resolve provisioning issues there using native UI and deploy to your devices.

### Running individual test suites
- The following shows how to run tests for a specific library:
```
./dotnet.sh build src/libraries/System.Numerics.Vectors/tests /t:Test /p:TargetOS=iOS /p:TargetArchitecture=x64
```

### Running the functional tests

There are [functional tests](https://github.com/dotnet/runtime/tree/main/src/tests/FunctionalTests/) which aim to test some specific features/configurations/modes on a target mobile platform.

A functional test can be run the same way as any library test suite, e.g.:
```
./dotnet.sh build /t:Test /p:TargetOS=iOSSimulator /p:TargetArchitecture=x64 /p:Configuration=Release src/tests/FunctionalTests/iOS/Simulator/PInvoke/iOS.Simulator.PInvoke.Test.csproj
```

Currently functional tests are expected to return `42` as a success code so please be careful when adding a new one.

### Testing various configurations

It's possible to test various configurations by setting a combination of additional MSBuild properties such as `RunAOTCompilation`,`MonoEnableInterpreter`, and some more.

1. Interpreter Only

This configuration is necessary for hot reload scenarios.

To enable the interpreter, add `/p:RunAOTCompilation=true /p:MonoEnableInterpreter=true` to a build command.

2. AOT only

To build for AOT only mode, add `/p:RunAOTCompilation=true /p:MonoEnableInterpreter=false` to a build command.

3. AOT-LLVM

To build for AOT-LLVM mode, add `/p:RunAOTCompilation=true /p:MonoEnableInterpreter=false /p:MonoEnableLLVM=true` to a build command.

### Test App Design
iOS/tvOS `*.app` (or `*.ipa`) is basically a simple [ObjC app](https://github.com/dotnet/runtime/blob/main/src/mono/msbuild/AppleAppBuilder/Templates/main-console.m) that inits the Mono Runtime. This Mono Runtime starts a simple xunit test
runner called XHarness.TestRunner (see https://github.com/dotnet/xharness) which runs tests for all `*.Tests.dll` libs in the bundle. There is also XHarness.CLI tool to deploy `*.app` and `*.ipa` to a target (device or simulator) and listens for logs via network sockets.

### Existing Limitations
- Simulator uses JIT mode only at the moment (to be extended with FullAOT and Interpreter)
- Interpreter is not enabled yet.
