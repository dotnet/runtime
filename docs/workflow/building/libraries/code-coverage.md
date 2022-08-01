Code Coverage
=============

"Code coverage" is a measure that indicates how much of our library code is exercised by our test suites. We measure code coverage using the [Coverlet](https://github.com/tonerdo/coverlet) global tool and use [ReportGenerator](https://github.com/danielpalme/ReportGenerator) to visualize that data. Daily/regular reports are currently not available.

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

Issues are opened for a library when a cursory examination of its code coverage reveal that there are likely still some meaningful gaps that need to be addressed.  We welcome contributions to our test suites to help address these gaps and close these issues.  Many of these issues are marked as [up-for-grabs](https://github.com/dotnet/runtime/labels/up-for-grabs).

An issue need not be addressed in its entirety. We happily accept contributions that improve our tests and work towards improving code coverage numbers even if they only incrementally improve the situation.

## Local Code Coverage Runs

You can perform code coverage runs for the entire repository locally by adding the `coverage` switch (assuming that  source and test assemblies are already built):

    build libs.tests -test -coverage

This runs the tests and generates the full code coverage report. The resulting index.htm file providing the results of the run should be available at:

    artifacts\coverage\index.htm

You can also build and test with code coverage for a particular test project rather than for the whole repo with the `/p:Coverage=true` property:

    dotnet build /t:Test /p:Coverage=true

By default, coverage is only provided for the library being tested.  However, if you want coverage data also gathered for the tests themselves, you can add `/p:CoverageIncludeTests=true`:

    dotnet build /t:test /p:Coverage=true /p:CoverageIncludeTests=true

The results for this one library will then be available in this index.htm file, where $(OutDir) is the directory where the binaries were generated.

    $(OutDir)\report\index.htm

For example, to build, test, and get code coverage results for the System.Diagnostics.Debug library, from the root of the repo one can do:

    dotnet build src\System.Diagnostics.Debug\tests /t:Test /p:Coverage=true

And then once the run completes:

    $(OutDir)\report\index.htm

## Code coverage with System.Private.CoreLib code

Some of the libraries for which contracts and tests live in libraries are actually fully or partially implemented in the core runtime library, e.g. the implementation that backs the System.Runtime contract is in System.Private.CoreLib.dll. Test projects for code that lives, fully or partially, in System.Private.CoreLib, should have the property `TestRuntime` set to `true` in order to obtain proper code coverage reports.

If the test project does not set the property `TestRuntime` to `true` and you want to collect code coverage that includes types in System.Private.CoreLib.dll add `/p:TestRuntime=true` to the coverage build command listed above.
