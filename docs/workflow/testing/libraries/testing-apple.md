# Testing Libraries on iOS and tvOS

In order to build libraries and tests for iOS or tvOS you need recent version of XCode installed (e.g. 11.3 or higher).

Build Libraries for iOS Simulator:
```
./build.sh mono+libs -os iOSSimulator -arch x64
```
Run tests one by one for each test suite on a simulator:
```
./build.sh libs.tests -os iOSSimulator -arch x64 -test
```
In order to run the tests on a device:
- Set the os to `iOS` instead of `iOSSimulator`
- Specify `DevTeamProvisioning` (see [developer.apple.com/account/#/membership](https://developer.apple.com/account/#/membership), scroll down to `Team ID`):
```
./build.sh libs.tests -os iOS -arch x64 -test /p:DevTeamProvisioning=H1A2B3C4D5
```
[AppleAppBuilder](https://github.com/dotnet/runtime/blob/main/src/mono/msbuild/AppleAppBuilder/AppleAppBuilder.cs) generates temp Xcode projects you can manually open and resolve provisioning issues there using native UI and deploy to your devices.

### Running individual test suites
- The following shows how to run tests for a specific library:
```
./dotnet.sh build src/libraries/System.Numerics.Vectors/tests /t:Test /p:TargetOS=iOS /p:TargetArchitecture=x64
```

### Test App Design
iOS/tvOS `*.app` (or `*.ipa`) is basically a simple [ObjC app](https://github.com/dotnet/runtime/blob/main/src/mono/msbuild/AppleAppBuilder/Templates/main-console.m) that inits the Mono Runtime. This Mono Runtime starts a simple xunit test
runner called XHarness.TestRunner (see https://github.com/dotnet/xharness) which runs tests for all `*.Tests.dll` libs in the bundle. There is also XHarness.CLI tool to deploy `*.app` and `*.ipa` to a target (device or simulator) and listens for logs via network sockets.

### Existing Limitations
- Most of the test suites crash on devices due to #35674
- Simulator uses JIT mode only at the moment (to be extended with FullAOT and Interpreter)
- Interpreter is not enabled yet.
