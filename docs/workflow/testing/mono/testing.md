# Running test suites using Mono

Before running tests, [build Mono](../../building/mono/README.md) using the desired configuration.

## Runtime Tests
### Testing against Mono JIT:

* Build runtime tests by executing the follwiing command from `$(REPO_ROOT)/src/tests`
```
./build.sh excludemonofailures <release|debug>
```

* Running a single test:
```
cd ../mono/netcore
make run-tests-coreclr CoreClrTest="bash ../../artifacts/tests/coreclr/OSX.x64.Release/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh"
```

 * Running all runtime tests:
```
cd ../mono/netcore
make run-tests-coreclr-all
```

### Testing on WebAssembly:
```
$(REPO_ROOT)/src/tests/build.sh -skipstressdependencies -excludemonofailures os Browser wasm <Release/Debug>
```

The last few lines of the build log should contain something like this:
```
--------------------------------------------------
 Example run.sh command

 src/tests/run.sh --coreOverlayDir=<repo_root>artifacts/tests/coreclr/Browser.wasm.Release/Tests/Core_Root --testNativeBinDir=<repo_root>/artifacts/obj/coreclr/Browser.wasm.Release/tests --testRootDir=<repo_root>/artifacts/tests/coreclr/Browser.wasm.Release --copyNativeTestBin Release
--------------------------------------------------
```

Run that command, adding `wasm` to the end.

### Testing on Android x64:
```
$(REPO_ROOT)/src/tests/build.sh -skipstressdependencies -excludemonofailures os Android x64 <Release/Debug>
```

The last few lines of the build log should contain something like this:
```
--------------------------------------------------
 Example run.sh command

 src/tests/run.sh --coreOverlayDir=<repo_root>artifacts/tests/coreclr/Android.x64.Release/Tests/Core_Root --testNativeBinDir=<repo_root>/artifacts/obj/coreclr/Android.x64.Release/tests --testRootDir=<repo_root>/artifacts/tests/coreclr/Android.x64.Release --copyNativeTestBin Release
--------------------------------------------------
```
Run that command, adding `Android` at the end.

For more details about internals of the runtime tests, please refer to the [CoreCLR testing documents](../coreclr)

## Libraries tests
* Build and run library tests against Mono JIT:
    * cd into the test library of your choice (`cd $(REPO_ROOT)/src/libraries/<library>/tests`)
    * Run the tests

```
$(REPO_ROOT)/dotnet.sh build /t:Test /p:RuntimeFlavor=mono /p:Configuration=<Release/Debug>
```

* Build and run library tests against Webassembly, Android or iOS. See instructions located in [Library testing document folder](../libraries/)

# Running the Mono samples
There are a few convenient samples located in `$(REPO_ROOT)/src/mono/netcore/sample`, which could help you test your program easily with different flavors of Mono or do a sanity check on the build.

## Sample for desktop Mono
It lives in `HelloWorld` folder. 

To run that program, you could simply cd to that directory and execute

```
make run
```

Note that, it is configured with run with `Release` and `LLVM` mode by default. If you would like to work with other mode, you could edit the Makefile.

## Sample for WebAssembly
It lives in `wasm` folder. There are two sub-folders `browser` and `console`. One is set up to run the program in browser, the other is set up to run the program in console.

To run that program, you could simply cd to the desirable sub-folder and execute

```
make build && make run
```

Note that, it is configured with run with `Release` mode by default. If you would like to work with other mode, you could edit the Makefile.

## Sample for Android
It lives in `Android` folder. 

To run that program, you could simply cd to that directory and execute

```
make run
```

Note that, it is configured with run with `x64` architecture and `Release` mode by default. If you would like to work with other configurations, you could edit the Makefile.

## Sample for iOS
It lives in `Android` folder. 

To run that program, you could simply cd to that directory and execute

```
make run
```

Note that, it is configured with run with `x64` architecture and `Debug` mode by default. If you would like to work with other configurations, you could edit the Makefile.