# Contributing

General information on contributing to dotnet projects is in the [Contributing Guide](https://github.com/Microsoft/dotnet/blob/master/CONTRIBUTING.md). This document contains information about coding styles, source structure, making pull requests, and more.

# Contributing to the corefx repo

## Required Software

* Install [Visual Studio 2013 Desktop Express with Update 3](http://www.microsoft.com/en-us/download/details.aspx?id=43733) or Visual Studio 2015 Community Preview.

## Building

* Open a [Visual Studio Command Prompt](http://msdn.microsoft.com/en-us/library/ms229859(v=vs.110).aspx).
* From the root of the repository, type `build`. This will build everything and run the core tests for the project.
* Visual Studio Solution (.sln) files exist for related groups of libraries. These can be loaded in Visual Studio and run builds from within Visual Studio.

## Testing

* By default, the core tests are run as part of the build. A test report for the build will be output on the console at the end of a successful build.
* To view and run the tests for a solution in Visual Studio you need to install **xUnit.net runner for Visual Studio**.
	* In Visual Studio under `Tools->Extensions and Updates`, select `Online` on the left side and search for `xUnit`. Download the **xUnit.net runner for Visual Studio**.
	* After downloading and restarting Visual Studio and building the test assemblies, the tests should start to show up in Test Explorer.  