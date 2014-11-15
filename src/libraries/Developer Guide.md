# Building, Testing, and Running

You can build .NET Core either via the command line or by using Visual Studio.
We currently only support building and running on Windows. Other platforms will
come later.

## Required Software

Install [Visual Studio 2013 Desktop Express with Update 3](http://www.microsoft.com/en-us/download/details.aspx?id=43733) 
or [Visual Studio Community 2013](http://go.microsoft.com/fwlink/?LinkId=517284).
You can also use the [Visual Studio 2015 Preview] (http://www.microsoft.com/en-us/download/details.aspx?id=44934).

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
[xunit-runner]: https://xunit.codeplex.com/wikipage?title=HowToUseVs2012

