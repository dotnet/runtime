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

You can also run the tests from within Visual Studio. See [this page][xunit-runner] 
for more details on how to install the **xUnit.net runner for Visual
Studio** test runner and how you can invoke the tests using Test Explorer.

[xunit]: http://xunit.github.io/
[xunit-runner]: http://xunit.github.io/docs/running-v1-tests-in-vs.html

