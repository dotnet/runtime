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

Please make sure your device is running a 64-bit aarch64 kernel.

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

At this point, you should be able to run a 32-bit `corerun`. You can verify this by downloading and running a recently built arm32 coreclr.

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

build-test.sh will have setup the Core_Root directory correctly after the test build.

```bash
~/coreclr$ tests/runtest.sh x64 checked
```

Please use the following command for help.

>./tests/runtest.sh -h

### Results

Test results will go into:

> `~/test/Windows_NT.x64.Debug/coreclrtests.xml`

### Unsupported and temporarily disabled tests

Unsupported tests outside of Windows have two annotations in their csproj to
ignore them when run.

```
<TestUnsupportedOutsideWindows>true</TestUnsupportedOutsideWindows>
```

This will write in the bash target to skip the test by returning a passing value if run outside Windows.

In addition:
```
<DisableProjectBuild Condition="'$(TargetsUnix)' == 'true'">true</DisableProjectBuild>
```

Is used to disable the build, that way if building on Unix cycles are saved building/running.

PAL tests
---------

Build CoreCLR on the Unix machine.

Run tests:

> `~/coreclr$ src/pal/tests/palsuite/runpaltests.sh ~/coreclr/bin/obj/Linux.x64.Debug`

Test results will go into:

> `/tmp/PalTestOutput/default/pal_tests.xml`
