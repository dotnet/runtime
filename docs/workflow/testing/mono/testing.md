# Running test suites using Mono

Before running tests, [build Mono](../../building/mono/README.md) using the desired configuration.

## Running Runtime Tests
* Build and run runtime tests against Mono JIT:

    * Build and run one specific runtime test. From the root directory run the following command:
```
cd src/mono/netcore
make run-tests-coreclr CoreClrTest="bash ../../artifacts/tests/coreclr/OSX.x64.Release/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh"
```

    * Build and run all runtime tests. From the root directory run the following command:
```
cd src/mono/netcore
make run-tests-coreclr-all
```

### Testing on WebAssembly:
```
src/tests/build.sh -skipstressdependencies -excludemonofailures os Browser wasm <Release/Debug>
```

The last few lines of the build log should contain something like this:
```
--------------------------------------------------
 Example run.sh command

 src/tests/run.sh --coreOverlayDir=<repo_root>artifacts/tests/coreclr/Browser.wasm.Release/Tests/Core_Root --testNativeBinDir=<repo_root>/artifacts/obj/coreclr/Browser.wasm.Release/tests --testRootDir=<repo_root>/artifacts/tests/coreclr/Browser.wasm.Release --copyNativeTestBin Release
--------------------------------------------------
```

Run that command, adding `wasm` to the end.

* Build and run Runtime Tests for Android x64. From the root directory run the following command:
```
src/tests/build.sh -skipstressdependencies -excludemonofailures os Android x64 <Release/Debug>
```
    * From the last few lines of the build log, you will see something like this
```
--------------------------------------------------
 Example run.sh command

 src/tests/run.sh --coreOverlayDir=<repo_root>artifacts/tests/coreclr/Browser.wasm.Release/Tests/Core_Root --testNativeBinDir=<repo_root>/artifacts/obj/coreclr/Browser.wasm.Release/tests --testRootDir=<repo_root>/artifacts/tests/coreclr/Browser.wasm.Release --copyNativeTestBin Release
--------------------------------------------------
```
    * To run the tests, copy that command and add `Android` at the end.

For more details about internals of the runtime tests, please refer to the [CoreCLR testing documents](../coreclr)

## Running Library Tests
* Build and run library tests against Mono JIT:

1. cd into the test library of your choice (`cd src/libraries/<library>/tests`)

2. Run the tests

```
dotnet build /t:Test /p:RuntimeFlavor=mono /p:Configuration=<Release/Debug>
```

* Build and run library tests against Webassembly. See instructions for [Testing Webassembly](../libraries/testing-wasm.md)

* Build and run library tests against Android. See instructions for [Testing Android](../libraries/testing-android.md)

* Build and run library tests against iOS. See instructions for [Testing iOS](../libraries/testing-apple.md)

# Test with sample program
There is a HelloWorld sample program lives at
```
$(REPO_ROOT)/src/mono/netcore/sample/HelloWorld
```

This is a good way to write simple test programs and get a glimpse of how mono will work with the dotnet tooling.

To run that program, you could simply cd to that directory and execute

```
make run
```

Note that, it is configured with run with `Release` and `LLVM` mode by default. If you would like to work with other modes, 
you could edit the Makefile from that folder.
