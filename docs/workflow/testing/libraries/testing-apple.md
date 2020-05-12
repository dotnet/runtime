# Testing Libraries on iOS and tvOS

In order to build librarires and tests for iOS or tvOS you only need some fresh version of XCode installed (e.g. 11.3 or higher).

Build Librarires for iOS:
```
./build.sh -os iOS -arch x64 -subset Mono+Libs
```
Run tests one by one for each test suite on a simulator:
```
./build.sh -os iOS -arch x64 -subset Libs.Tests -test
```
Unlike XHarness-Android, XHarness for iOS is able to boot simulators itself.

### Running individual test suites
- The following shows how to run tests for a specific library on a simulator
```
cd src/libraries/System.Numerics.Vectors/tests
../../../.././dotnet.sh build /t:Test /p:TargetOS=iOS /p:TargetArchitecture=x64
```
for devices you need to specify `DevTeamProvisioning`:
```
cd src/libraries/System.Numerics.Vectors/tests
../../../.././dotnet.sh build /t:Test /p:TargetOS=iOS /p:TargetArchitecture=arm64 /p:DevTeamProvisioning=...
```

### Known Issues
- Most of the test suites crash on devices due to [#35674)(https://github.com/dotnet/runtime/issues/35674)