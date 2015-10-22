Building and running tests on Linux, OS X, and FreeBSD
======================================================

CoreCLR tests
-------------

**Building**

Build CoreCLR and CoreFX. Refer to building instructions in the respective repository.

To build only the tests, on the Windows machine:

> `C:\coreclr>tests\buildtest.cmd clean`

**Running tests**

The following instructions assume that on the Unix machine:
- The CoreCLR repo is cloned at `~/coreclr`
- The CoreFX repo is cloned at `~/corefx`
- The Windows clone of the CoreCLR repo is mounted at `/media/coreclr`
- The Windows clone of the CoreFX repo is mounted at `/media/corefx`

Tests currently need to be built on Windows and copied over to the Unix machine for testing. Copy the test build over to the Unix machine:

> `cp --recursive /media/coreclr/bin/tests/Windows_NT.x64.Debug ~/test/`

See runtest.sh usage information:

> `~/coreclr$ tests/runtest.sh --help`

Run tests:

> ```bash
> ~/coreclr$ tests/runtest.sh
>     --testRootDir=~/test/Windows_NT.x64.Debug
>     --testNativeBinDir=~/coreclr/bin/obj/Linux.x64.Debug/tests
>     --coreClrBinDir=~/coreclr/bin/Product/Linux.x64.Debug
>     --mscorlibDir=/media/coreclr/bin/Product/Linux.x64.Debug
>     --coreFxBinDir=~/corefx/bin/Linux.AnyCPU.Debug
>     --coreFxNativeBinDir=~/corefx/bin/Linux.x64.Debug
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
