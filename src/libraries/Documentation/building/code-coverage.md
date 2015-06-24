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

To include code coverage in this run, augment it with the ```/p:Coverage=true``` argument:

    build /p:Coverage=true

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
