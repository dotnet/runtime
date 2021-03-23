Building and running tests on Linux, macOS, and FreeBSD
======================================================

CoreCLR tests
-------------

## Building

Build CoreCLR on [Unix](../../building/coreclr/linux-instructions.md).

## Building the Tests

Dotnet CLI is required to build the tests. This can be done on any platform then copied over if the architecture or OS does not support Dotnet.

To build the tests on Unix:

```sh
./src/tests/build.sh
```

By default, the test build uses Release as the libraries configuration. To use a different configuration, set the `LibrariesConfiguration` property to the desired configuration. For example:

```
./src/tests/build.sh /p:LibrariesConfiguration=Debug
```

Please note that this builds the Priority 0 tests. To build priority 1:

```sh
./src/tests/build.sh -priority1
```

## Generating Core_Root

The `src/tests/build.sh` script generates the Core_Root folder, which contains the test host (`corerun`), libraries, and coreclr product binaries necessary to run a test. To generate Core_Root without building the tests:

```
./src/tests/build.sh generatelayoutonly
```

The output will be at `<repo_root>/artifacts/tests/coreclr/<os>.<arch>.<configuration>/Tests/Core_Root`.


## Building Individual Tests

During development there are many instances where building an individual test is fast and necessary. All of the necessary tools to build are under `coreclr`. It is possible to use `~/runtime/dotnet.sh msbuild` as you would normally use MSBuild with a few caveats.

**!! Note !! -- Passing /p:TargetOS=[OSX|Linux] is required.**

## Building an Individual Test

```sh
./dotnet.sh msbuild src/tests/path-to-proj-file /p:TargetOS=<TargetOS> /p:Configuration=<BuildType>
```

In addition to the test assembly, this will generate a `.sh` script next to the test assembly in the test's output folder. The test's output folder will be under `<repo_root>/artifacts/tests/coreclr/<os>.<arch>.<configuration>` at a subpath based on the test's location in source.

## Running Tests

The following instructions assume that on the Unix machine:
- The CoreCLR repo is cloned at `/mnt/coreclr`

`src/tests/build.sh` will have set up the `Core_Root` directory correctly after the test build.

```sh
./src/tests/run.sh x64 checked
```

Please use the following command for help.

```sh
./src/tests/run.sh -h
```

### Unsupported and temporarily disabled tests

To support building all tests for all targets on single target, we use
the conditional property

```xml
<CLRTestTargetUnsupported Condition="...">true</CLRTestTargetUnsupported>
```

This property disables building of a test in a default build. It also
disables running a test in the bash/batch wrapper scripts. It allows the
test to be built on any target in CI when the `allTargets` option is
passed to the `build.*` scripts.

Tests which never should be built or run are marked

```xml
<DisableProjectBuild>true</DisableProjectBuild>
```

This propoerty should not be conditioned on Target properties to allow
all tests to be built for `allTargets`.

## Running Individual Tests

After [building an individual test](#building-individual-tests), to run the test:

1) Set the `CORE_ROOT` environment variable to the [Core_Root folder](#generating-core_root).

2) Run the test using the `.sh` generated for the test.

PAL tests
---------

Build CoreCLR with PAL tests on the Unix machine:

```sh
./build.sh clr.paltests
```

Run tests:

To run all tests including disabled tests
```sh
./src/coreclr/pal/tests/palsuite/runpaltests.sh $(pwd)/artifacts/bin/coreclr/$(uname).x64.Debug/paltests
# on macOS, replace $(uname) with OSX
```
To only run enabled tests for the platform the tests were built for:
```sh
artifacts/bin/coreclr/$(uname).x64.Debug/paltests/runpaltests.sh $(pwd)/artifacts/bin/coreclr/$(uname).x64.Debug/paltests
# on macOS, replace $(uname) with OSX
```

Test results will go into: `/tmp/PalTestOutput/default/pal_tests.xml`

To disable tests in the CI edit
`src/coreclr/pal/tests/palsuite/issues.targets`
