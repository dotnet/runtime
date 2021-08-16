# Running test suites using Mono

Before running tests, [build Mono](../../building/mono/README.md) using the desired configuration.

## Runtime Tests
### Desktop Mono:

To build the runtime tests for Mono JIT or interpreter, build CoreCLR and execute the following command from `$(REPO_ROOT)/src/tests`
```
./build.sh excludemonofailures <release|debug>
```

Run individual test:
```
cd src/mono
make run-tests-coreclr CoreClrTest="bash ../../artifacts/tests/coreclr/OSX.x64.Release/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh"
```

Run all tests:
```
cd src/mono
make run-tests-coreclr-all
```

### WebAssembly:
Build the runtime tests for WebAssembly
```
$(REPO_ROOT)/src/tests/build.sh -excludemonofailures os Browser wasm <Release/Debug>
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
$(REPO_ROOT)/src/tests/build.sh -excludemonofailures os Android <x64/arm64> <Release/Debug>
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
### Mobile targets and WebAssembly
Build and run library tests against Webassembly, Android or iOS. See instructions located in [Library testing document folder](../libraries/)

# Running the Mono samples
There are a few convenient samples located in `$(REPO_ROOT)/src/mono/sample`, which could help you test your program easily with different flavors of Mono or do a sanity check on the build. The samples are set up to work with a specific configuration; please refer to the relevant Makefile for specifics. If you would like to work with a different configuration, you can edit the Makefile.

## Desktop Mono
To run the desktop Mono sample, cd to `HelloWorld` and execute:

```
make run
```
Note that the default configuration of this sample is LLVM JIT.

## WebAssembly
To run the WebAssembly sample, cd to `wasm`.  There are two sub-folders `browser` and `console`. One is set up to run the progam in browser, the other is set up to run the program in console. Enter the desirable sub-folder and execute

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
