# Testing Libraries on iOS and tvOS

In order to build librarires and tests for iOS or tvOS you only need some fresh version of XCode installed (e.g. 11.3 or higher).

Build Librarires for iOS:
```
./build.sh mono+libs -os iOS -arch x64
```
Run tests one by one for each test suite on a simulator:
```
./build.sh -os iOS -arch x64 -subset Libs.Tests -test
```
Unlike XHarness-Android, XHarness for iOS is able to boot simulators by its own.

### Running individual test suites
- The following shows how to run tests for a specific library on a simulator
```
cd src/libraries/System.Numerics.Vectors/tests
~runtime/dotnet.sh build /t:Test /p:TargetOS=iOS /p:TargetArchitecture=x64
```
for devices you need to specify `DevTeamProvisioning`:
```
cd src/libraries/System.Numerics.Vectors/tests
../../../.././dotnet.sh build /t:Test /p:TargetOS=iOS /p:TargetArchitecture=arm64 /p:DevTeamProvisioning=...
```
AppleAppBuilder generates temp Xcode projects you can manually open and resolve provisioning there using native UI and deploy to your devices and start debugging.

### Known Issues
- Most of the test suites crash on devices due to [#35674)(https://github.com/dotnet/runtime/issues/35674)
