You can build .NET Core either via the command line or by using Visual Studio.
We currently only support building and running on Windows. Other platforms will
come later.

## Required Software

Visual Studio 2013 (Update 3 or later) or Visual Studio 2015 (Preview or later) is required.

The following free downloads are compatible:
* [Visual Studio Community 2013 (with Update 3)](http://www.visualstudio.com/en-us/visual-studio-community-vs.aspx)
* [Visual Studio Ultimate 2015 Preview](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs)

## Building From the Command Line

Open a [Visual Studio Command Prompt](http://msdn.microsoft.com/en-us/library/ms229859(v=vs.110).aspx). 
From the root of the repository, type `build`. This will build everything and run
the core tests for the project. Visual Studio Solution (.sln) files exist for
related groups of libraries. These can be loaded to build, debug and test inside
the Visual Studio IDE.

## Tests

We use the OSS testing framework [xunit|http://xunit.github.io/]

### Running tests on the command line

By default, the core tests are run as part of the build. Running the tests from
the command line is as simple as invoking `build.cmd`. 

You can also run the test for an individual project by building just one test
project, e.g.:
```
cd src\System.Collections.Immutable\tests
msbuild /t:BuildAndTest (or /t:Test to just re-run the tests)
```
In some test directories there may be multiple test projects or directories so you may need to specify the specific test project to get it to build and run the tests.

### Running tests from Visual Studio

1. Open solution of interest
2. Right click test project and select 'Set as startup project'
3. Ctrl+F5 (Run)

### Debugging tests in Visual Studio

1. Install VS 2015 Preview or later including Web Developer Tools
2. Open solution of interest in VS 2015
3. Right click test project and select 'Set as startup project'
4. Set breakpoint appropriately
5. F5 (Debug)

### Code Coverage

Code coverage is built into the corefx build system.  It utilizes OpenCover for generating coverage data and ReportGenerator for generating reports about that data.  To run:

```
// Run full coverage
build.cmd /p:Coverage=true

// To run a single project with code coverage enabled pass the /p:Coverage=true property
cd src\System.Collections.Immutable\tests
msbuild /t:BuildAndTest /p:Coverage=true
```
If coverage succeeds, the code coverage report will be generated automatically and placed in the bin\tests\coverage directory.  You can view the full report by opening index.htm

Code coverage reports from the continuous integration system are available from the links on the front page of the corefx repo.

### Notes 
* Running tests from using the VS test explorer does not currently work 
after we switched to running on CoreCLR. We will be working on enabling 
full VS test integration but we don't have an ETA yet. In the meantime,
use the steps above to launch/debug the tests using the console runner.

* VS 2015 is required to debug tests running on CoreCLR as the CoreCLR
debug engine is a VS 2015 component.
