Building and running tests on Windows
=====================================

## Building Tests

Building coreclr tests must be done using a specific script as follows:

```
src\tests\build.cmd
```

By default, the test build uses Release as the libraries configuration. To use a different configuration, set the `LibrariesConfiguration` property to the desired configuration. For example:

```
src\tests\build.cmd /p:LibrariesConfiguration=Debug
```

## Building Native Test Components

Sometimes you want to only build the native test components instead of the managed and native components. To build the native test components only, pass the `skipmanaged` and `skipgeneratelayout` parameters to the build script as follows:

```
src\tests\build.cmd skipmanaged skipgeneratelayout
```

## Building C++/CLI native test components against the live ref assemblies

By default, the C++/CLI native test components build against the ref pack from the SDK specified in the `global.json` file in the root of the repository. To build these components against the ref assemblies produced in the build, pass the `-cmakeargs -DCPP_CLI_LIVE_REF_ASSEMBLIES=1` parameters to the test build. For example:

```
src\tests\build.cmd skipmanaged -cmakeargs -DCPP_CLI_LIVE_REF_ASSEMBLIES=1
```

## Building Precompiled Tests

```
src\tests\build.cmd crossgen
```

This will use `crossgen.exe` to precompile test executables before they are executed.

## Building Specific Priority Tests

```
src\tests\build.cmd -priority=1
```

The above is an example of requesting that priority '1' and below be built. The default priority value is '0'. If '1' is specified, all tests with `CLRTestPriorty` `0` **and** `1` will be built and run.

## Generating Core_Root

The `src\tests\build.cmd` script generates the Core_Root folder, which contains the test host (`corerun`), libraries, and coreclr product binaries necessary to run a test. To generate Core_Root without building the tests:

```
src\tests\build.cmd generatelayoutonly
```

The output will be at `<repo_root>\artifacts\tests\coreclr\windows.<arch>.<configuration>\Tests\Core_Root`. For example, the location for x64 checked would be: `<repo_root>\artifacts\tests\coreclr\windows.x64.Checked\Tests\Core_Root`

## Examples

To build crossgen'd priority '0' and '1' tests:

```
src\tests\build.cmd crossgen -priority=1
```

To generate Core_Root for x86 release without building tests:

```
src\tests\build.cmd x86 Release generatelayoutonly
```

For additional supported parameters use the following:

```
src\tests\build.cmd -?
```

## Building Test Subsets

The `src\tests\build.cmd` script supports three options that let you limit the set of tests to build;
when none of these is specified, the entire test tree (under `src\tests`) gets built but that can be
lengthy (especially in `-priority=1` mode) and unnecessary when working on a particular test.

1) `test <test-project>` - build a particular test project specified by its project file path,
either absolute or relative to `src\tests`. The option can be specified multiple times on the command
line to request building several individual projects; alternatively, a single occurrence of the option
can represent several project paths separated by semicolons.

**Example**: `src\tests\build.cmd test JIT/Methodical/divrem/div/i4div_cs_do.csproj;JIT/Methodical/divrem/div/i8div_cs_do.csproj`

2) `dir <test-folder>` - build all test projects within a given directory path, either absolute
or relative to `src\tests`. The option can be specified multiple times on the command line to request
building projects in several folders; alternatively, a single instance of the option
can represent several project folders separated by semicolons.

**Example**: `src\tests\build.cmd dir JIT/Methodical/Arrays/huge;JIT/Methodical/divrem/div`

3) `tree <root-folder>` - build all test projects within the subtree specified by its root path,
either absolute or relative to `src\tests`. The option can be specified multiple times on the command
line to request building projects in several subtrees; alternatively, a single instance of the option
can represent several project subtree root folder paths separated by semicolons.

**Example**: `src\tests\build.cmd tree baseservices/exceptions;JIT/Methodical`

**Please note** that priority filtering is orthogonal to specifying test subsets; even when you request
building a particular test and the test is Pri1, you need to specify `-priority=1` in the command line,
otherwise the test build will get skipped. While somewhat unintuitive, 'fixing' this ad hoc would likely
create more harm than good and we're hoping to ultimately get rid of the test priorities in the long term
anyway.

## Building Individual Tests

**Note:** `build.cmd skipmanaged [Any additional flags]` needs to be run at least once if the individual test has native assets.

* Native Test: Build the generated Visual Studio solution or makefile corresponding to test cmake file.

* Managed Test: Use `dotnet.cmd` from the root of the repo on the test project directly.

In addition to the test assembly, this will generate a `.cmd` script next to the test assembly in the test's output folder. The test's output folder will be under `<repo_root>\artifacts\tests\coreclr\windows.<arch>.<configuration>` at a subpath based on the test's location in source.

## Running Tests

Will list supported parameters.

```
src\tests\run.cmd /?
```

In order to run all of the tests using your checked build:

```
src\tests\run.cmd checked
```

This will generate a report named `TestRun_<arch>_<flavor>.html` (e.g. `TestRun_windows_x64_Checked.html`) in the subdirectory `<repo_root>\artifacts\log`. Any tests that failed will be listed in `TestRunResults_windows_x64_Checked.err`.

### Investigating Test Failures

Upon completing a test run, you may find one or more tests have failed.

The output of the test will be available in `Test` reports directory, but by default the directory will be something like `<repo_root>\artifacts\tests\coreclr\windows.x64.Checked\Reports\Exceptions\Finalization`.

There are 2 files of interest:

- `Finalizer.output.txt` - Contains all the information logged by the test.
- `Finalizer.error.txt` - Contains the information reported by CoreRun.exe (which executed the test) when the test process crashes.

To re-run a failed test, follow the instructions for [running individual tests](#running-individual-tests). The test report for the failed test will contain the test command to run - for example, `<repo_root>\artifacts\tests\coreclr\windows.x64.Checked\Exceptions\Finalization\Finalizer.cmd`.

## Running Individual Tests

After [building an individual test](#building-individual-tests), to run the test:

1) Set the `CORE_ROOT` environment variable to the [Core_Root folder](#generating-core_root).

2) Run the test using the `.cmd` generated for the test.

If you wish to run the test under a debugger (e.g. [WinDbg](http://msdn.microsoft.com/library/windows/hardware/ff551063(v=vs.85).aspx)), append `-debug <debuggerFullPath>` to the test command.

## Modifying a test

If test changes are needed, make the change, and re-build the test project. This will binplace the binaries in the test binaries folder (e.g. `<repo_root>\artifacts\tests\coreclr\windows.x64.Checked\Exceptions\Finalization`). Then re-run the test following the instructions above.
