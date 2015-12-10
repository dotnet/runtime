# CLR Garbage Collector Performance Tests
This folder houses both the test framework and test artifacts for performance tests
targeting the garbage collector. These tests are run using the 
[xunit-performance](https://github.com/Microsoft/xunit-performance) performance testing
framework and can be used with the standard tools provided by that repository.

The performance tests themselves, as defined in the `Framework` folder in `PerfTests.cs`,
all invoke one of the test artifacts (as defined in the `Tests` assembly) and collects the duration
in which the child process runs, as well as a number of other metrics on Windows platforms.

## Building the test framework
The test framework currently does not build as part of the CoreCLR test build. The framework
builds using the `dnu` build tool in order to target both DNX46 (Desktop) or DNXCORE5 (CoreCLR)
depending on the platform on which the tests are to be invoked.

The Desktop (DNX46) target of the test framework contains a number of custom metrics that are given
to `xunit.performance` to evaluate the test run. These metrics provide a number of interesting
statistics about the performance of the GC, like the duration of the longest pause, the average pause
durations, and number of garbage collections for each generation.

The CoreCLR (DNXCORE5) target of the test framework consists only of the tests themselves and not
the metrics. This is because metric definitions have a dependency on TraceEvent, which is itself
not available currently on CoreCLR.

## Running the tests on Windows
Since the Desktop CLR is already installed on Windows machines, we can use the host CLR to
invoke the `xunit.performance.run` test runner, even if we are testing CoreCLR.

Regardless of whether or not we are testing the Desktop CLR or CoreCLR, we need to copy all of our
test dependencies to the same location, some sort of scratch folder:

```
mkdir sandbox
pushd sandbox

REM Get the xunit-performance console runner
xcopy /s C:\<path_to_your_coreclr>\coreclr\tests\packages\Microsoft.DotNet.xunit.performance.runner.Windows\1.0.0-alpha-build0025\tools\* .

REM Get the xunit-performance analysis engine
xcopy /sy C:\<path_to_your_coreclr>\coreclr\tests\packages\Microsoft.DotNet.xunit.performance.analysis\1.0.0-alpha-build0025\tools\* .

REM Get the xunit console runner
xcopy /sy C:\<path_to_your_coreclr>\coreclr\tests\packages\xunit.console.netcore\1.0.2-prerelease-00128\runtimes\any\native\* .

REM Get the test executables' dependencies
xcopy /sy C:\<path_to_your_coreclr>\coreclr\bin\tests\Windows_NT.x64.Release\Tests\Core_Root\* .

REM Get the test executables themselves
for /r C:\<path_to_your_coreclr>\coreclr\bin\tests\Windows_NT.x64.Release\GC\Performance\Tests\ %ff in (*) do xcopy "%%f" .

REM Get the test framework assembly
xcopy /sy C:\<path_to_your_coreclr>\coreclr\tests\src\GC\Performance\Framework\bin\Debug\dnx46\* .
```

Once all of our dependencies are in the same place, we can run the tests:
```
xunit.performance.run.exe Framework.dll -runner xunit.console.exe -verbose -runid PerformanceTest
```

In order to test CoreCLR, we need to set two environment variables: `GC_PERF_TEST_CORE_RUN_PROBE_PATH`, indicating
where to look for `CoreRun.exe`, and `GC_PERF_TEST_CORECLR`, which when set to "1" indicates the test runner
to launch subprocesses under `CoreRun`. All other commands should be exactly the same. (See the Environment Variables
section for more details on what environment variables the test framework respects).

The result of this invocation will be `PerformanceTest.etl`, an ETW trace, and `PerformanceTest.xml`, a file
containing a summary of every test run and the metrics that were calculated for every test iteration. A summary
XML file can be created using the analysis executable:

```
xunit.performance.analysis.exe PerformanceTest.xml -xml PerformanceTestSummary.xml
```

## Running on other platforms
In order to run performance tests on other platforms, it's necessary to obtain the required components as
specified above, possibly from an existing CoreCLR build on Windows. However, there are three major differences:

First, instead of using the `xunit.performance.run.exe` obtained from the 
`Microsoft.DotNet.xunit.performance.runner.Windows` nuget package, we must instead install the command:

```
dnu commands install Microsoft.DotNet.xunit.performance.runner.dnx 1.0.0-alpha-build0027 -f https://www.myget.org/F/dotnet-buildtools/
```

Second, instead of using the `xunit.console.exe` test runner, we must use `xunit.console.netcore.exe`, which
is available as part of the CoreCLR test build, through NuGet at https://www.myget.org/F/dotnet-buildtools/, or
on GitHub: https://github.com/dotnet/buildtools/tree/master/src/xunit.console.netcore.

Finally, we must use the `dnxcore5` target when building the test framework, since custom metrics are not available
on non-Windows platforms currently.

With all of the above in place, we can run:

```
xunit.performance.run.exe Framework.dll -verbose -runner ./xunit.console.netcore.exe -runnerhost ./corerun -runid PerformanceTest.xml
```

Only the Duration metric will be available in the resulting XML.

## Environment Variables
On Windows, the test runner respects the following environment variables:
* `GC_PERF_TEST_PROBE_PATH`, a path used to probe for test executables,
* `GC_PERF_TEST_CORE_RUN_PROBE_PATH`, a path used to probe for the CoreRun executable if running on CoreCLR,
* `GC_PERF_TEST_CORECLR`, instructs the runner to use CoreCLR (and CoreRun) if set to 1.

On Unixes, the test runner respects the same variables except for the final variable, which is assumed to
always be 1 on non-Windows platforms.