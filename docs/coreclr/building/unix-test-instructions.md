Building and running tests on Linux, OS X, and FreeBSD
======================================================

CoreCLR tests
-------------

**Building**

Build CoreCLR on [Unix](https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md), and CoreFX on [Unix](https://github.com/dotnet/corefx/blob/master/Documentation/building/unix-instructions.md).

**Building the Tests**

To build the tests on Unix:

> `./build-test.sh -rebuild`

As of [#11860](https://github.com/dotnet/coreclr/pull/11860) building the tests on unix works; however, it will take significantly more time than building on Windows.

To build on Windows:

> `C:\coreclr>build-test.cmd -rebuild`

**Building Individual Tests**

During development there are many instances where building an individual test is fast and necessary. All of the necessary tools to build are under coreclr/Tools. It is possible to use coreclr/Tools/MSBuild.dll as you would normally use MSBuild with a few caveats.

Note that coreclr/Tools/msbuild.sh exists as well to make the call shorter.

**!! Note !! -- Passing /p:__BuildOs=[OSX|Linux] is required.** 

>If you omit it you will get the following error: `error MSB4801: The task factory "CodeTaskFactory" could not be loaded because this version of MSBuild does not support it.`
---

**Building an Individual Test Example**

>`coreclr/Tools/msbuild.sh /maxcpucount  coreclr/tests/src/JIT/CodeGenBringUpTests/Array1.csproj /p:__BuildType=Release /p:__BuildOS=OSX`

Or

>`coreclr/Tools/dotnetcli/dotnet coreclr/Tools/MSBuild.dll /maxcpucount coreclr/tests/src/JIT/CodeGenBringUpTests/Array1.csproj /p:__BuildType=Release /p:__BuildOS=OSX`


**Running tests**

The following instructions assume that on the Unix machine:
- The CoreCLR repo is cloned at `~/coreclr`
- The CoreFX repo is cloned at `~/corefx`
- The Windows clone of the CoreCLR repo is mounted at `/media/coreclr`

Tests currently need to be built on Windows and copied over to the Unix machine for testing. Copy the test build over to the Unix machine:

> `cp --recursive /media/coreclr/bin/tests/Windows_NT.x64.Debug ~/test/`

See runtest.sh usage information:

> `~/coreclr$ tests/runtest.sh --help`

Run tests (`Debug` may be replaced with `Release` or `Checked`, depending on which Configuration you've built):

> ```bash
> ~/coreclr$ tests/runtest.sh
>     --testRootDir=~/test/Windows_NT.x64.Debug
>     --testNativeBinDir=~/coreclr/bin/obj/Linux.x64.Debug/tests
>     --coreClrBinDir=~/coreclr/bin/Product/Linux.x64.Debug
>     --mscorlibDir=~/coreclr/bin/Product/Linux.x64.Debug
>     --coreFxBinDir=~/corefx/bin/runtime/netcoreapp-Linux-Debug-x64
> ```

The method above will copy dependencies from the set of directories provided to create an 'overlay' directory.
If you already have an overlay directory prepared with the dependencies you need, you can specify `--coreOverlayDir`
instead of `--coreClrBinDir`, `--mscorlibDir`, `--coreFxBinDir`, and `--coreFxNativeBinDir`. It would look something like:


> ```bash
> ~/coreclr$ tests/runtest.sh
>     --testRootDir=~/test/Windows_NT.x64.Debug
>     --testNativeBinDir=~/coreclr/bin/obj/Linux.x64.Debug/tests
>     --coreOverlayDir=/path/to/directory/containing/overlay
> ```


Test results will go into:

> `~/test/Windows_NT.x64.Debug/coreclrtests.xml`

**Unsupported and temporarily disabled tests**

These tests are skipped by default:
- Tests that are not supported outside Windows, are listed in:
>> `~/coreclr/tests/testsUnsupportedOutsideWindows.txt`
- Tests that are temporarily disabled outside Windows due to unexpected failures (pending investigation), are listed in:
>> `~/coreclr/tests/testsFailingOutsideWindows.txt`

To run only the set of temporarily disabled tests, pass in the `--runFailingTestsOnly` argument to `runtest.sh`.

PAL tests
---------

Build CoreCLR on the Unix machine.

Run tests:

> `~/coreclr$ src/pal/tests/palsuite/runpaltests.sh ~/coreclr/bin/obj/Linux.x64.Debug`

Test results will go into:

> `/tmp/PalTestOutput/default/pal_tests.xml`
