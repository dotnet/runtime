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

During development there are many instances where building an individual test is fast and necessary. All of the necessary tools to build are under `coreclr`. It is possible to use `coreclr/.dotnet/dotnet msbuild` as you would normally use MSBuild with a few caveats.

**!! Note !! -- Passing /p:__BuildOs=[OSX|Linux] is required.** 

## Building an Individual Test

>`/path/to/coreclr/.dotnet/dotnet msbuild tests/src/path-to-proj-file /p:__BuildOS=<BuildOS> /p:__BuildType=<BuildType>`

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
