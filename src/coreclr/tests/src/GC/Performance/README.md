# CLR Garbage Collector Performance Tests
This folder houses both the test framework and test artifacts for performance tests
targeting the garbage collector. These tests are run using the
[xunit-performance](https://github.com/Microsoft/xunit-performance) performance testing
framework and can be used with the standard tools provided by that repository.

The performance tests themselves, as defined in the `Framework` folder in `PerfTests.cs`,
all invoke one of the test artifacts (as defined in the `Tests` assembly) and collects the duration
in which the child process runs, as well as a number of other metrics on Windows platforms.

## Building the test framework
The test framework currently does not build as part of the CoreCLR test build. The
framework targets Desktop CLR and compiles using msbuild.

The Desktop (DNX46) target of the test framework contains a number of custom metrics that are given
to `xunit.performance` to evaluate the test run. These metrics provide a number of interesting
statistics about the performance of the GC, like the duration of the longest pause, the average pause
durations, and number of garbage collections for each generation.

The CoreCLR (DNXCORE5) target of the test framework consists only of the tests themselves and not
the metrics. This is because metric definitions have a dependency on TraceEvent, which is itself
not available currently on CoreCLR. This target is temporarily disabled for now.

## Running the tests on Windows
Since the Desktop CLR is already installed on Windows machines, we can use the host CLR to
invoke the `xunit.performance.run` test runner, even if we are testing CoreCLR.

Regardless of whether or not we are testing the Desktop CLR or CoreCLR, we first need to set up
the coreclr repo by building a build that we will be testing:

```
build.cmd Release
tests\runtest.cmd Release GenerateLayoutOnly
```

Then, we create a temporary directory somewhere on our system and set up all of our dependencies:

```
mkdir sandbox
pushd sandbox

REM Get the xunit-performance console runner
xcopy /sy C:\<path_to_your_coreclr>\coreclr\packages\Microsoft.DotNet.xunit.performance.runner.Windows\1.0.0-alpha-build0035\tools\* .

REM Get the xunit-performance analysis engine
xcopy /sy C:\<path_to_your_coreclr>\coreclr\packages\Microsoft.DotNet.xunit.performance.analysis\1.0.0-alpha-build0035\tools\* .

REM Get the xunit console runner
xcopy /sy C:\<path_to_your_coreclr>\coreclr\packages\xunit.runner.console\2.1.0\tools\* .

REM Get the test executables' dependencies
xcopy /sy C:\<path_to_your_coreclr>\coreclr\bin\tests\Windows_NT.x64.Release\Tests\Core_Root\* .

REM Get the test executables themselves
for /r C:\<path_to_your_coreclr>\coreclr\bin\tests\Windows_NT.x64.Release\GC\Performance\Tests\ %%f in (*) do xcopy /sy "%%f" .

REM Get the test framework assembly
xcopy /sy C:\<path_to_your_coreclr>\coreclr\tests\src\GC\Performance\Framework\bin\Release\* .

REM Instruct the framework to 1) run using CoreRun (coreclr) and 2) find CoreRun in the current directory
REM If not set, the framework will test the currently running Desktop CLR instead.
set GC_PERF_TEST_CORECLR=1
set GC_PERF_TEST_CORE_RUN_PROBE_PATH=.
```

Once all of our dependencies are in the same place, we can run the tests:
```
xunit.performance.run.exe GCPerfTestFramework.dll -runner xunit.console.exe -verbose -runid PerformanceTest
```

The result of this invocation will be `PerformanceTest.etl`, an ETW trace, and `PerformanceTest.xml`, a file
containing a summary of every test run and the metrics that were calculated for every test iteration. A summary
XML file can be created using the analysis executable:

```
xunit.performance.analysis.exe PerformanceTest.xml -xml PerformanceTestSummary.xml
```

This summary XML only contains test durations and discards all custom metrics.

## Running on other platforms
The GC performance test framework is temporarily not available on non-Windows platform. It will be brought to
non-Windows platforms in the very near future!

## Environment Variables
On Windows, the test runner respects the following environment variables:
* `GC_PERF_TEST_PROBE_PATH`, a path used to probe for test executables,
* `GC_PERF_TEST_CORE_RUN_PROBE_PATH`, a path used to probe for the CoreRun executable if running on CoreCLR,
* `GC_PERF_TEST_CORECLR`, instructs the runner to use CoreCLR (and CoreRun) if set to 1.

On Unixes, the test runner respects the same variables except for the final variable, which is assumed to
always be 1 on non-Windows platforms.