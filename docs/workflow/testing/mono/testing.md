# Running test suites using Mono

Before running tests, [build Mono](../../building/mono/README.md) using the desired configuration.

## Runtime Tests
### Desktop Mono:

To build the runtime tests for Mono JIT or interpreter:

1. Build test host (corerun) - From the `$(REPO_ROOT)`:

```
./build.sh clr.hosts -c <release|debug>
```

2. Build the tests (in `$(REPO_ROOT)/src/tests`)

```
cd src/tests
./build.sh mono <release|debug>
```

To build an individual test, test directory, or a whole subdirectory tree, use the `-test:`, `-dir:` or `-tree:` options (without the src/tests prefix)
For example: `./build.sh mono release -test:JIT/opt/InstructionCombining/DivToMul.csproj`


Run individual test:
```
bash ./artifacts/tests/coreclr/OSX.x64.Release/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh -coreroot=`pwd`/artifacts/tests/coreclr/OSX.x64.Release/Tests/Core_Root
```

Run all built tests:
```
./run.sh <Debug|Release>
```

To debug a single test with `lldb`:

1. Run the shell script for the test case manually with the `-debug` option:
```
bash ./artifacts/tests/coreclr/OSX.x64.Release/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh -coreroot=`pwd`/artifacts/tests/coreclr/OSX.x64.Release/Tests/Core_Root -debug=/usr/bin/lldb
```
2. In LLDB add the debug symbols for mono: `add-dsym <CORE_ROOT>/libcoreclr.dylib.dwarf`
3. Run/debug the test


### WebAssembly:
Build the runtime tests for WebAssembly
```
$(REPO_ROOT)/src/tests/build.sh -mono os Browser wasm <Release/Debug>
```

The last few lines of the build log should contain something like this:
```
--------------------------------------------------
 Example run.sh command

 src/tests/run.sh --coreOverlayDir=<repo_root>artifacts/tests/coreclr/Browser.wasm.Release/Tests/Core_Root --testNativeBinDir=<repo_root>/artifacts/obj/coreclr/Browser.wasm.Release/tests --testRootDir=<repo_root>/artifacts/tests/coreclr/Browser.wasm.Release --copyNativeTestBin Release
--------------------------------------------------
```

To run all tests, execute that command, adding `wasm` to the end.

### Android:
Build the runtime tests for Android x64/ARM64
```
$(REPO_ROOT)/src/tests/build.sh -mono os Android <x64/arm64> <Release/Debug>
```

Run one test wrapper from repo root
```
export CORE_ROOT=<path_to_folder_Core_Root>
./dotnet.sh <path_to_xunit.console.dll> <path_to_*.XUnitWrapper.dll>
```

### Additional Documents
For more details about internals of the runtime tests, please refer to the [CoreCLR testing documents](../coreclr)

## Libraries tests
### Desktop Mono
Build and run library tests against Mono JIT or interpreter
```
$(REPO_ROOT)/dotnet.sh build /t:Test /p:RuntimeFlavor=mono /p:Configuration=<Release/Debug> $(REPO_ROOT)/src/libraries/<library>/tests
```
Alternatively, you could execute the following command from `$(REPO_ROOT)/src/mono`
```
make run-tests-corefx-<library>
```
For example, the following command is for running System.Runtime tests:
```
make run-tests-corefx-System.Runtime
```

### Debugging libraries tests on Desktop Mono

See [debugging with VS Code](../../debugging/libraries/debugging-vscode.md#Debugging-Libraries-with-Visual-Studio-Code-running-on-Mono)

### Mobile targets and WebAssembly
Build and run library tests against WebAssembly, Android or iOS. See instructions located in [Library testing document folder](../libraries/)

## Running the functional tests

There are the [functional tests](https://github.com/dotnet/runtime/tree/main/src/tests/FunctionalTests/) which aim to test some specific features/configurations/modes on Android, iOS-like platforms (iOS/tvOS + simulators, MacCatalyst), and WebAssembly.

A functional test can be run the same way as any library test suite, e.g.:
```
./dotnet.sh build /t:Test -c Release /p:TargetOS=Android /p:TargetArchitecture=x64 src/tests/FunctionalTests/Android/Device_Emulator/PInvoke/Android.Device_Emulator.PInvoke.Test.csproj
```

Currently the functional tests are expected to return `42` as a success code so please be careful when adding a new one.

For more details, see instructions located in [Library testing document folder](../libraries/).

# Running the Mono samples
There are a few convenient samples located in `$(REPO_ROOT)/src/mono/sample`, which could help you test your program easily with different flavors of Mono or do a sanity check on the build. The samples are set up to work with a specific configuration; please refer to the relevant Makefile for specifics. If you would like to work with a different configuration, you can edit the Makefile.

## Desktop Mono
To run the desktop Mono sample, cd to `HelloWorld` and execute:

```
make run
```
Note that the default configuration of this sample is LLVM JIT.

## WebAssembly
To run the WebAssembly sample, cd to `wasm`.  There are two sub-folders `browser` and `console`. One is set up to run the program in browser, the other is set up to run the program in console. Enter the desirable sub-folder and execute

```
make build && make run
```

## Android
To run the Android sample, cd to `Android` and execute

```
make run
```

## iOS
To run the iOS sample, cd to `iOS` and execute

```
make run
```
