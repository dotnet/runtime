Code Coverage
=============

"Code coverage" is a measure that indicates how much of our library code is exercised by our test suites.  We measure code coverage using the [OpenCover](https://github.com/opencover/opencover), and a report of our latest code coverage results can be seen by clicking the coverage badge on the [CoreFX home page](https://github.com/dotnet/corefx), linking to the latest [Coverage Report](https://ci.dot.net/job/dotnet_corefx/job/master/job/code_coverage_windows/Code_Coverage_Report/).

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

Issues are opened for a library when a cursory examination of its code coverage reveal that there are likely still some meaningful gaps that need to be addressed.  We welcome contributions to our test suites to help address these gaps and close these issues.  Many of these issues are marked as [up-for-grabs](https://github.com/dotnet/corefx/labels/up-for-grabs).

An issue need not be addressed in its entirety.  We happily accept contributions that improve our tests and work towards improving code coverage numbers even if they only incrementally improve the situation.

## Automated Code Coverage Runs

Code coverage runs are performed by Jenkins approximately twice a day.  The results of these runs are all available from the site linked to by the code coverage badge on the home page.

## PR Code Coverage Runs

Jenkins can create a coverage report using your PR. Ask for it using `@dotnet-bot test code coverage please`.

After it's done the report can be found in the build log, it looks like eg
`https://ci.dot.net/job/dotnet_corefx/job/master/job/code_coverage_windows_prtest/16/artifact/bin/tests/coverage`
then add index.htm on the end:
`https://ci.dot.net/job/dotnet_corefx/job/master/job/code_coverage_windows_prtest/16/artifact/bin/tests/coverage/index.htm`
You can navigate to this from your PR by clicking the "Details" link to the right of the code coverage job listed at the bottom of the PR after having issued the above request to dotnet-bot.  In the Jenkins UI for the resulting build, click the "Build Artifacts" link and navigate through the resulting hierarchy to the index.htm file.

## Local Code Coverage Runs

You can perform code coverage runs locally on your own machine.  Normally to build your entire CoreFX repo, from the root of your repo you'd run:

    build -includetests

To include code coverage in this run, augment the `build -includetests` call with the `coverage` argument:

    build -includetests -coverage

This will do the build and testing as with the normal ```build```, but it will run the tests using the OpenCover tool.  A resulting index.htm file providing the results of the run will be available at:

    bin\tests\coverage\index.htm

You can also build and test with code coverage for a particular test project rather than for the whole repo.  Normally to build and test a particular test suite, from the same directory as that test suite's .csproj, you'd run:

    dotnet msbuild /t:BuildAndTest

To do so with code coverage, append the ```/p:Coverage=true``` argument:

    dotnet msbuild /t:BuildAndTest /p:Coverage=true

The results for this one library will then show up in the aforementioned index.htm file. For example, to build, test, and get code coverage results for the System.Diagnostics.Debug library, from the root of the repo one can do:

    cd src\System.Diagnostics.Debug\tests\
    dotnet msbuild /t:BuildAndTest /p:Coverage=true

And then once the run completes:

    ..\..\..\bin\tests\coverage\index.htm

## Code coverage with System.Private.CoreLib code

Some of the libraries for which contracts and tests live in the corefx repo are actually fully or partially implemented in the core runtime library in another repo, e.g. the implementation that backs the System.Runtime contract is in System.Private.CoreLib.dll in either the coreclr or corert repo. Test projects for code that lives, fully or partially, in System.Private.CoreLib, should have the property `TestRuntime` set to `true` in order to obtain proper code coverage reports.

If the test project does not set the property `TestRuntime` to `true` and you want to collect code coverage that includes types in System.Private.CoreLib.dll add `/p:CodeCoverageAssemblies="System.Private.CoreLib"` to the coverage build command listed above.

If you want to get coverage report against a private build of System.Private.CoreLib follow the steps outlined at [Testing with Private CoreClr Bits](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md#testing-with-private-coreclr-bits).

The build and test projects take care of copying assemblies and PDBs as needed for coverage runs. The resulting code coverage report should now also include details for System.Private.CoreLib.

Note: as of 10/2017 OpenCover, the default coverage tool, requires PDBs to be Windows PDBs - the needed conversions are automatically performed by coverage runs. You can determine if it is a Windows PDB by doing 'more System.Private.CoreLib.pdb',  if it begins with 'Microsoft C/C++ MSF 7.00' it is a Windows PDB.  If you need a Windows PDB the Pdb2Pdb tool will convert (or you can do a dotnet msbuild /t:rebuild /p:DebugType=full).

## Cross-platform Coverage
As of 07/2018 CoreFx is only able to get coverage information on Windows. To correct this we are experimenting with [coverlet](https://github.com/tonerdo/coverlet).

### Know Issues ###

1. Instrumenting "System.Private.CoreLib" is causing test to crash (both on Windows and Unix).

### Windows Instructions ###
On Windows by default coverage runs will use OpenCover instead of coverlet, use the build property `UseCoverlet` to change this default. Currently the use of `dotnet msbuild` is required to avoid a problem with one of the coverlet dependencies. Here is the command:

```
dotnet msbuild /t:RebuildAndTest /p:Coverage=True /p:UseCoverlet=True
```

### Unix Instructions ###
On Unix just specifying `/p:Coverage=True` triggers the usage of coverlet. However, in order to generate the html report a few setup steps are needed.

1. Install the [dotnet/cli global tool reportgenerator](https://www.nuget.org/packages/dotnet-reportgenerator-globaltool), add it to the PATH if not automatically added by dotnet/cli (it can be only for the current session)
2. On Linux install the Arial font required for report generation: `sudo apt-get install ttf-mscorefonts-installer`

After that you request a coverage run from a test folder, e.g.:

```
dotnet msbuild /t:RebuildAndTest /p:Coverage=True
```

Open index.htm located at bin/tests/coverage to see the results of the coverage run.
