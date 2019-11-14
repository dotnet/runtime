Building and running tests on Windows
=====================================

## Building Tests

To build the tests simply navigate to the tests directory above the repo and run,

    C:\git\coreclr>build-test.cmd

## Cleaning Tests

**Note:** Cleaning should be done before all tests to be sure that the test assets are initialized correctly. To do a clean build of the tests, in a clean command prompt, issue the following command:

    C:\git\coreclr>build-test.cmd -rebuild

## Building Tests that will be precompiled

    C:\git\coreclr>build-test.cmd crossgen

This will use `crossgen.exe` to precompile the test executables before they are executed.

## Building Other Priority Tests

    C:\git\coreclr>build-test.cmd -priority=1

The number '1' is just an example. The default value (if no priority is specified) is 0. To clarify, if '1' is specified, all tests with CLRTestPriorty 0 **and** 1 will be built and consequently run.

## Examples

To run a clean, priority 1, crossgen test pass:

    C:\git\coreclr>build-test.cmd -rebuild crossgen -priority=1

`build-test.cmd /?` - will list additional supported parameters.

### Building Individual Tests

Note: build-test.cmd or build.cmd skipnative needs to be run at least once

* Native Test: Build the generated Visual Studio solution or make file corresponding to Test cmake file.

* Managed Test: You can invoke msbuild on the project directly from Visual Studio Command Prompt.

### Running Tests

`runtest.cmd /?` - will list supported parameters.

For example to run all of the tests using your checked build:

     <repo_root>\tests\runtest.cmd checked

This will generate a report named as `TestRun_<arch>_<flavor>.html` (e.g. `TestRun_Windows_NT__x64__Checked.html`) in the subdirectory `<repo_root>\bin\Logs`. Any tests that failed will be listed in `TestRunResults_Windows_NT__x64__Checked.err`.

### Investigating Test Failures

Upon completing a test run, you may find one or more tests have failed.

The output of the Test will be available in Test reports directory, but the default the directory would be something like is `<repo_root>\bin\tests\Windows_NT.x64.Checked\Reports\Exceptions\Finalization`.

There are 2 files of interest:

- `Finalizer.output.txt` - Contains all the information logged by the test.
- `Finalizer.error.txt`  - Contains the information reported by CoreRun.exe (which executed the test) when the test process crashes.

### Rerunning a failed test

If you wish to re-run a failed test, please follow the following steps:

1. Set an environment variable, `CORE_ROOT`, pointing to the path to product binaries that was passed to runtest.cmd.
For example using a checked build the location would be: `<repo_root>\bin\tests\Windows_NT.x64.Checked\Tests\Core_Root`

1. Next, run the failed test, the command to which is also present in the test report for a failed test. It will be something like `<repo_root>\bin\tests\Windows_NT.x64.Checked\Exceptions\Finalization\Finalizer.cmd`.

If you wish to run the test under a debugger (e.g. [WinDbg](http://msdn.microsoft.com/en-us/library/windows/hardware/ff551063(v=vs.85).aspx)), append `-debug <debuggerFullPath>` to the test command. For example:

     <repo_root>\bin\tests\Windows_NT.x64.Checked\Exceptions\Finalization\Finalizer.cmd -debug <debuggerFullPath>

### Modifying a test

If test changes are needed, make the change and build the test project. This will binplace the binaries in test binaries folder (e.g. `<repo_root>\bin\tests\Windows_NT.x64.Checked\Exceptions\Finalization`). At this point, follow the steps to re-run a failed test to re-run the modified test.

