Code Coverage
=============

"Code coverage" is a measure that indicates how much of our library code is exercised by our test suites.  We measure code coverage using the [OpenCover](https://github.com/opencover/opencover), and a report of our latest code coverage results can be seen by clicking the coverage badge on the [CoreFX home page](https://github.com/dotnet/corefx):

[![Coverage status](https://img.shields.io/badge/coverage-report-blue.svg)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_coverage_windows/lastBuild/Code_Coverage_Report/)

This report shows each library currently being tested with code coverage and provides statistics around the quality of the code coverage for the library.  It also provides a line-by-line breakdown of what lines are being covered and what lines are not.

## Goals

The code coverage report provides a percentage value per library of the number of source lines exercised by the tests.  There is no hard and fast percentage that must be obtained per library, as every library is unique and comes with its own set of intricacies and constraints.  While in some cases it's possible and reasonable to achieve 100% code coverage, this is rare.  There are many valid reasons certain pieces of code won't be exercised in tests, e.g.:
- A code file is compiled into multiple projects, and only some of the code is used in each project.
- Code exists to handle rare race conditions too costly to simulate in normal conditions.
- Code exists to handle particular machine/OS configurations that are not used during code coverage runs.

Etc.  What's important is that the right set of tests exist to ensure that the code is behaving properly and that regressions in functionality can be caught quickly, and code coverage metrics are a way to help guide us to that end.

Our default, somewhat-arbitrary initial goal for a library is 90% code coverage.  That doesn't mean we're done with testing once a library hits 90%, nor does it mean we must keep going with a library until it hits 90%.  We use this metric and the associated coverage information to help guide us towards the ideal for a given library.

(Note that we do not want to arbitrarily inflate our code coverage numbers.  Tests must provide value in and of themselves and should not simply be written in a haphazard manner meant to execute more lines of code without providing real value.)

## Issues

Issues are opened for a library when a cursory examination of its code coverage reveal that there are likely still some meaningful gaps that need to be addressed.  We welcome contributions to our test suites to help address these gaps and close these issues.  Many of these issues are marked as "up for grabs".

An issue need not be addressed in its entirety.  We happily accept contributions that improve our tests and work towards improving code coverage numbers even if they only incrementally improve the situation.

## Automated Code Coverage Runs

Code coverage runs are performed by Jenkins approximately twice a day.  The results of these runs are all available from the site linked to by the code coverage badge on the home page.

## Local Code Coverage Runs

You can perform code coverage runs locally on your own machine.  Normally to build your entire CoreFX repo, from the root of your repo you'd run:

    build
    build-tests

To include code coverage in this run, augment the `build-tests` call with the `coverage` argument:

    build
    build-tests -coverage

This will do the build and testing as with the normal ```build```, but it will run the tests using the OpenCover tool.  A resulting index.htm file providing the results of the run will be available at:

    bin\tests\coverage\index.htm

You can also build and test with code coverage for a particular test project rather than for the whole repo.  Normally to build and test a particular test suite, from the same directory as that test suite's .csproj, you'd run:

    msbuild /t:BuildAndTest

To do so with code coverage, as with ```build``` append the ```/p:Coverage=true``` argument:

    msbuild /t:BuildAndTest /p:Coverage=true

The results for this one library will then also show up in the aforementioned index.htm file. For example, to build, test, and get code coverage results for the System.Diagnostics.Debug library, from the root of my repo I can do:

    cd src\System.Diagnostics.Debug\tests\
    msbuild /t:BuildAndTest /p:Coverage=true
    
And then once the run completes:
    
    ..\..\..\bin\tests\coverage\index.htm

## Code coverage with mscorlib code

Some of the libraries for which contracts and tests live in the corefx repo are actually implemented in the core runtime library in another repo, e.g. the implementation that backs the System.Runtime contract is in System.Private.Corlib.dll in either the coreclr or corert repo. To run coverage reports for these projects, you need to build mscorlib locally from the coreclr repo.

The following steps can be used manually to produce a coverage report, but a customizable batch file can be found [here](facade-code-coverage.bat). Changing the parameters in the first couple of lines lets you run a coverage report easily for any facade project.

1. Build the local test project (`msbuild /T:Build`)
3. Build coreclr locally in Debug or Release (`build.cmd all Debug skiptests`)
2. Navigate to the built test directory in the corefx bin (e.g. `bin/tests/AnyOS.AnyCPU.Debug/System.Runtime/netcoreapp1.0` for `System.Runtime`
4. Delete `coreclr.dll`, `mscorlib.dll`, `mscorlib.ni.dll`, `System.Private.CoreLib.dll` and `System.Private.CoreLib.ni.dll` from that directory
5. Copy all files in the coreclr `bin` directory to the test directory
6. Copy all files in the coreclr `bin/PDB` directory to the test directory
7. Run an OpenCover command with `xunit.console.netcore.exe`. For example:

	<corefx-root>/packages/OpenCover/<opencover-version>/tools/OpenCover.Console.exe -oldStyle -filter:"+[*]* -[*.Tests]* -[xunit.*]*" -excludebyfile:"*\Common\src\System\SR.*" -nodefaultfilters -excludebyattribute:*.ExcludeFromCodeCoverage* -skipautoprops -hideskipped:All -threshold:1 -returntargetcode -register:user -targetdir:<path-to corefx-bin> -target:CoreRun.exe -output:coverage.xml -targetargs:"xunit.console.netcore.exe System.Runtime.Tests -xml testResults.xml -notrait Benchmark=true -notrait category=OuterLoop -notrait category=failing -notrait category=nonwindowstests"

8. Run a ReportGenerator command with the generated `coverage.xml` file. For example:

	<corefx-root>/packages/ReportGenerator/<opencover-version>/tools/ReportGenerator.exe -reporttypes:Html;Badges -reports:coverage.xml
