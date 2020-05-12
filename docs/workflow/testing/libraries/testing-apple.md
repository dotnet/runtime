# Testing Libraries on iOS and tvOS

In order to build libraries and tests for iOS or tvOS you only need some fresh version of XCode installed (e.g. 11.3 or higher).

Build Libraries for iOS:
```
./build.sh mono+libs -os iOS -arch x64
```
Run tests one by one for each test suite on a simulator:
```
./build.sh libs.tests -os iOS -arch x64 -test
```
Unlike XHarness-Android, XHarness for iOS is able to boot simulators by its own.

### Running individual test suites
- The following shows how to run tests for a specific library on a simulator
```
cd src/libraries/System.Numerics.Vectors/tests
./dotnet.sh build /t:Test /p:TargetOS=iOS /p:TargetArchitecture=x64
```
for devices you need to specify `DevTeamProvisioning`:
```
./dotnet.sh build /t:Test /p:TargetOS=iOS /p:TargetArchitecture=arm64 /p:DevTeamProvisioning=...
```
AppleAppBuilder generates temp Xcode projects you can manually open and resolve provisioning there using native UI and deploy to your devices and start debugging.

### How the tests work
iOS/tvOS *.app (or *.ipa) is basically a simple [ObjC app](https://github.com/dotnet/runtime/blob/master/src/mono/msbuild/AppleAppBuilder/Templates/main-console.m) that inits the Mono Runtime. This Mono Runtime starts a simple xunit test
runner called XHarness TestRunner which runs tests for all *.Tests.dll libs in the bundle. There is also XHarness.CLI tool with `mlaunch` inside to deploy `*.app` and `*.ipa` to a target (device or simulator) and listens for logs via sockets.

### Known Issues
- Most of the test suites crash on devices due to [#35674)(https://github.com/dotnet/runtime/issues/35674)