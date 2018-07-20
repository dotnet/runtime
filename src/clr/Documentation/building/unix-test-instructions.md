Building and running tests on Linux, OS X, and FreeBSD
======================================================

CoreCLR tests
-------------

## Building

Build CoreCLR on [Unix](https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md).

## Building the Tests

DotNet is required to build the tests, this can be done on any platform then copied over if the arch or os does not support DotNet. If DotNet is not supported, [CoreFX](https://github.com/dotnet/corefx/blob/master/Documentation/building/unix-instructions.md) is also required to be built.

To build the tests on Unix:

> `./build-test.sh`

Please note that this builds the Priority 0 tests. To build priority 1:

> `build-test.sh -priority 1`

## Building Individual Tests

During development there are many instances where building an individual test is fast and necessary. All of the necessary tools to build are under `coreclr/Tools`. It is possible to use `coreclr/Tools/MSBuild.dll` as you would normally use MSBuild with a few caveats.

Note that `coreclr/Tools/msbuild.sh` exists as well to make the call shorter.

**!! Note !! -- Passing /p:__BuildOs=[OSX|Linux] is required.** 

## Building an Individual Test Example

>`coreclr/Tools/msbuild.sh /maxcpucount  coreclr/tests/src/JIT/CodeGenBringUpTests/Array1.csproj /p:__BuildType=Release /p:__BuildOS=OSX`

Or

>`coreclr/Tools/dotnetcli/dotnet coreclr/Tools/MSBuild.dll /maxcpucount coreclr/tests/src/JIT/CodeGenBringUpTests/Array1.csproj /p:__BuildType=Release /p:__BuildOS=OSX`

## Aarch64/armhf multi-arch

For machines that have aarch64/armhf support, all the armhf packages will need to also be downloaded. Please note you will need to enable multiplatform support as well. Check with your distro provider or kernel options to see if this is supported. For simplicity, these instructions relate to aarch64 ubuntu enabling arm32 (hf) coreclr runs.

Please make sure your device is running a 64 bit aarch64 kernel.

```
# Example output

[ubuntu:~]: uname -a
Linux tegra-ubuntu 4.4.38-tegra #1 SMP PREEMPT Thu Jul 20 00:41:06 PDT 2017 aarch64 aarch64 aarch64 GNU/Linux

```

```
# Enable armhf multiplatform support
[ubuntu:~]: sudo dpkg --add-architecture armhf
[ubuntu:~]: sudo apt-get update

[ubuntu:~]: sudo apt-get install libstdc++6:armhf
````

At this point you should be able to run a 32-bit `corerun`. You can verify this by downloading and running a recently built arm32 coreclr.

```
[ubuntu:~]: wget https://ci.dot.net/job/dotnet_coreclr/job/master/job/armlb_cross_checked_ubuntu/lastSuccessfulBuild/artifact/*zip*/archive.zip --no-check-certificate
[ubuntu:~]: unzip archive.zip
[ubuntu:~]: chmod +x && ./archive/bin/Product/Linux.arm.Checked/corerun
Execute the specified managed assembly with the passed in arguments

Options:
-c, --clr-path  path to the libcoreclr.so and the managed CLR assemblies
```

Now download the coreclr armhf dependencies.

```
sudo apt-get install libunwind8:armhf libunwind8-dev:armhf libicu-dev:armhf liblttng-ust-dev:armhf libcurl4-openssl-dev:armhf libicu-dev:armhf libssl-dev libkrb5-dev:armhf
```

## Running Tests

The following instructions assume that on the Unix machine:
- The CoreCLR repo is cloned at `/mnt/coreclr`

If DotNet is unsupported
- The CoreFX repo is cloned at `/mnt/corefx`
- The other platform's clone of the CoreCLR repo is mounted at `/media/coreclr`

The following steps are different if DotNet is supported or not on your arch and os.

### DotNet is supported

build-test.sh will have setup the Core_Root directory correctly after the test build. If this was either skipped or needs to be regenerated use:

>`build-test.sh generatelayoutonly`

To run the tests run with the --coreOverlayDir path

```bash
~/coreclr$ tests/runtest.sh
    --testRootDir=/mnt/coreclr/bin/tests/Linux.x64.Debug
    --testNativeBinDir=/mnt/coreclr/bin/obj/Linux.x64.Debug/tests
    --coreOverlayDir=/mnt/coreclr/bin/tests/Linux.x64.Debug/Tests/Core_Root
    --copyNativeTestBin
```

### DotNet is not supported

Tests need to be built on another platform and copied over to the Unix machine for testing. Copy the test build over to the Unix machine:

> `cp --recursive /media/coreclr/bin/tests/Windows_NT.x64.Debug /mnt/test/`

See `runtest.sh` usage information:

> `/mnt/coreclr$ tests/runtest.sh --help`

Run tests (`Debug` may be replaced with `Release` or `Checked`, depending on which Configuration you've built):

```bash
/mnt/coreclr$ tests/runtest.sh
    --testRootDir=/mnt/test/Windows_NT.x64.Debug
    --testNativeBinDir=/mnt/coreclr/bin/obj/Linux.x64.Debug/tests
    --coreClrBinDir=/mnt/coreclr/bin/Product/Linux.x64.Debug
    --mscorlibDir=/mnt/coreclr/bin/Product/Linux.x64.Debug
    --coreFxBinDir=/mnt/corefx/bin/runtime/netcoreapp-Linux-Debug-x64
```

The method above will copy dependencies from the set of directories provided to create an 'overlay' directory.

### Results

Test results will go into:

> `~/test/Windows_NT.x64.Debug/coreclrtests.xml`

### Unsupported and temporarily disabled tests

These tests are skipped by default:
- Tests that are not supported outside Windows, are listed in:
    > `~/coreclr/tests/testsUnsupportedOutsideWindows.txt`
- Tests that are temporarily disabled outside Windows due to unexpected failures (pending investigation), are listed in:
    > `~/coreclr/tests/testsFailingOutsideWindows.txt`

To run only the set of temporarily disabled tests, pass in the `--runFailingTestsOnly` argument to `runtest.sh`.

PAL tests
---------

Build CoreCLR on the Unix machine.

Run tests:

> `~/coreclr$ src/pal/tests/palsuite/runpaltests.sh ~/coreclr/bin/obj/Linux.x64.Debug`

Test results will go into:

> `/tmp/PalTestOutput/default/pal_tests.xml`
