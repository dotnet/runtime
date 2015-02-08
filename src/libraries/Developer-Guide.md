You can build .NET Core either via the command line or by using Visual Studio.
We currently only support building and running on Windows. Other platforms will
come later.

## Required Software

Visual Studio 2013 (Update 3 or later) or Visual Studio 2015 (Preview or later) is required.

The following free downloads are compatible:
* [Visual Studio Community 2013](http://www.visualstudio.com/en-us/visual-studio-community-vs.aspx)
* [Visual Studio Ultimate 2015 Preview](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs)

## Building From the Command Line

Open a [Visual Studio Command Prompt](http://msdn.microsoft.com/en-us/library/ms229859(v=vs.110).aspx). 
From the root of the repository, type `build`. This will build everything and run
the core tests for the project. Visual Studio Solution (.sln) files exist for
related groups of libraries. These can be loaded to build, debug and test inside
the Visual Studio IDE.

## Running Tests

We use the OSS testing framework [xUnit.net][xunit].

By default, the core tests are run as part of the build. Running the tests from
the command line is as simple as invoking `build.cmd`. A test report for the
build will be output on the console at the end of a successful build.

You can also run the test for an individual project by building just one test
project, e.g.:
```
cd src\System.Collections.Immutable\tests
msbuild
```

**NOTE: Running tests from using the VS test explorer does not currently work 
after we switched to running on CoreCLR.** 

We will be working on enabling full VS test integration but we don't have an
ETA yet. In the meantime, however, we do have basic test debugging support in 
Visual Studio 2015:

1. Install VS 2015 Preview or later including Web Developer Tools
2. Open solution of interest in VS 2015
3. Right click test project and select 'Set as startup project'
4. Set breakpoint appropriately
5. F5 (Debug)

[xunit]: http://xunit.github.io/
[xunit-runner]: http://xunit.github.io/docs/running-v1-tests-in-vs.html

