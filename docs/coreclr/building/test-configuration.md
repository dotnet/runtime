# General Test Infrastructure

## Test "Kind"

* Build Only
  * Builds an executable.
  * Will not execute.
  * e.g. `<CLRTestKind>BuildOnly</CLRTestKind>`
* Run Only
  * Can use output of `BuildOnly` or `BuildAndRun` projects with different command line arguments.
  * e.g. `<CLRTestKind>RunOnly</CLRTestKind>`
* Build And Run
  * Builds an executable.
  * Will execute said executable.
  * e.g. `<CLRTestKind>BuildAndRun</CLRTestKind>`
* Shared Libraries
  * For building libraries common to zero or more tests.
  * e.g. `<CLRTestKind>SharedLibrary</CLRTestKind>`

By default (i.e. if not specified explicitly), test "Kind" is `BuildAndRun`.

## Priority

Test cases are categorized by priority level. The most important subset should be and is the smallest subset. This subset is called priority 0.

* By default, a test case is priority 0. Tests must be explicitly de-prioritized.
* Set the priority of a test by setting the property `<CLRTestPriority>` in the test's project file.
  * e.g. `<CLRTestPriority>2</CLRTestPriority>`
* Lower priority values are always run in conjunction when running higher priority value tests.
  * i.e. if a developer elects to do a priority 2 test run, then all priority 0, 1 and 2 tests are run.

## Adding Test Guidelines

* All test source files should include the following banner:
```
    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    // See the LICENSE file in the project root for more information.
```
* Disable building of a test by conditionally setting the `<DisableProjectBuild>` property.
	* e.g. `<DisableProjectBuild Condition=" '$(Platform)' == 'arm64' ">true</DisableProjectBuild>`
* Add NuGet/MyGet references by updating the following [project file](https://github.com/dotnet/coreclr/blob/master/tests/src/Common/test_dependencies/test_dependencies.csproj).
* Build against the `mscorlib` facade by adding `<ReferenceLocalMscorlib>true</ReferenceLocalMscorlib>` to the test project.

### Creating a new C# test project

**TODO**

### Converting an existing C# project
  * Remove the `<AssemblyName>` property
  * Import `dir.props`
  * Import `dir.targets`
  * Assign a `<CLRTestKind>`
  * (optional) Assign a priority value