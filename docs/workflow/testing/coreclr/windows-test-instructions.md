Building and running tests on Windows
=====================================

## Building Tests

Building coreclr tests must be done using a specific script as follows:

```
> src\coreclr\build-test.cmd
```

## Building Precompiled Tests

```
> src\coreclr\build-test.cmd crossgen
```

This will use `crossgen.exe` to precompile test executables before they are executed.

## Building Specific Priority Tests

```
> src\coreclr\build-test.cmd -priority=1
```

The above is an example of requesting that priority '1' and below be built. The default priority value is '0'. If '1' is specified, all tests with `CLRTestPriorty` `0` **and** `1` will be built and run.

## Examples

To run a priority '0' and '1' and crossgen'd test pass:

```
> src\coreclr\build-test.cmd crossgen -priority=1
```

For additional supported parameters use the following:

```
> src\coreclr\build-test.cmd -?
```

### Building Individual Tests

**Note:** `build-test.cmd skipmanaged [Any additional flags]` needs to be run at least once if the individual test has native assets.

* Native Test: Build the generated Visual Studio solution or makefile corresponding to test cmake file.

* Managed Test: Use `dotnet.cmd` from the root of the repo on the test project directly.

### Running Tests

Will list supported parameters.

```
> src\coreclr\tests\runtest.cmd /?
```

In order to run all of the tests using your checked build:

```
> src\coreclr\tests\runtest.cmd checked
```

This will generate a report named `TestRun_<arch>_<flavor>.html` (e.g. `TestRun_Windows_NT_x64_Checked.html`) in the subdirectory `<repo_root>\artifacts\log`. Any tests that failed will be listed in `TestRunResults_Windows_NT_x64_Checked.err`.

### Investigating Test Failures

Upon completing a test run, you may find one or more tests have failed.

The output of the test will be available in `Test` reports directory, but by default the directory will be something like `<repo_root>\artifacts\tests\coreclr\Windows_NT.x64.Checked\Reports\Exceptions\Finalization`.

There are 2 files of interest:

- `Finalizer.output.txt` - Contains all the information logged by the test.
- `Finalizer.error.txt` - Contains the information reported by CoreRun.exe (which executed the test) when the test process crashes.

### Re-run a failed test

If you wish to re-run a failed test, follow the following steps:

1) Set an environment variable, `CORE_ROOT`, pointing to the path to product binaries that was passed to runtest.cmd.
For example using a checked build the location would be: `<repo_root>\artifacts\tests\coreclr\Windows_NT.x64.Checked\Tests\Core_Root`

2) Run the failed test, the command to which is also present in the test report for a failed test. It will be something like `<repo_root>\artifacts\tests\coreclr\Windows_NT.x64.Checked\Exceptions\Finalization\Finalizer.cmd`.

If you wish to run the test under a debugger (e.g. [WinDbg](http://msdn.microsoft.com/library/windows/hardware/ff551063(v=vs.85).aspx)), append `-debug <debuggerFullPath>` to the test command. For example:

```
> artifacts\tests\coreclr\Windows_NT.x64.Checked\Exceptions\Finalization\Finalizer.cmd -debug <debuggerFullPath>
```

### Modifying a test

If test changes are needed, make the change, and build the test project. This will binplace the binaries in the test binaries folder (e.g. `<repo_root>\artifacts\tests\coreclr\Windows_NT.x64.Checked\Exceptions\Finalization`). Then re-run the test following the instructions above.
