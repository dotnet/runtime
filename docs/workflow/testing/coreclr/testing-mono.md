# Running Runtime Tests using Mono Runtime

## Build mono
Before running any tests, build mono in the way that you would like to test with.
See the instructions for [Building Mono](../../building/mono/README.md)

## Build and Run Runtime Tests
* Build and run one specific Runtime Test for JIT. From the root directory run the following command:
```
cd src/mono/netcore
make run-tests-coreclr CoreClrTest="bash ../../artifacts/tests/coreclr/OSX.x64.Release/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh"
```

* Build and run all Runtime Tests for JIT. From the root directory run the following command:
```
cd src/mono/netcore
make run-tests-coreclr-all
```

* Build and run Runtime Tests for Webassembly. From the root directory run the following command:
```
src/tests/build.sh -skipstressdependencies -excludemonofailures os Browser wasm <Release/Debug>
```
From the last few lines of the build log, you will see something like this
```
--------------------------------------------------
 Example run.sh command

 src/tests/run.sh --coreOverlayDir=<repo_root>artifacts/tests/coreclr/Browser.wasm.Release/Tests/Core_Root --testNativeBinDir=<repo_root>/artifacts/obj/coreclr/Browser.wasm.Release/tests --testRootDir=<repo_root>/artifacts/tests/coreclr/Browser.wasm.Release --copyNativeTestBin Release
--------------------------------------------------
```
To run the tests, copy that command and add `wasm` at the end.

* Build and run Runtime Tests for Android x64. From the root directory run the following command:
```
src/tests/build.sh -skipstressdependencies -excludemonofailures os Android x64 <Release/Debug>
```
From the last few lines of the build log, you will see something like this
```
--------------------------------------------------
 Example run.sh command

 src/tests/run.sh --coreOverlayDir=<repo_root>artifacts/tests/coreclr/Browser.wasm.Release/Tests/Core_Root --testNativeBinDir=<repo_root>/artifacts/obj/coreclr/Browser.wasm.Release/tests --testRootDir=<repo_root>/artifacts/tests/coreclr/Browser.wasm.Release --copyNativeTestBin Release
--------------------------------------------------
```
To run the tests, copy that command and add `Android` at the end.
