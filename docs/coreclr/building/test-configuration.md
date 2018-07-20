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
* Add NuGet/MyGet references by updating the following [test project](https://github.com/dotnet/coreclr/blob/master/tests/src/Common/test_dependencies/test_dependencies.csproj).
* Build against the `mscorlib` facade by adding `<ReferenceLocalMscorlib>true</ReferenceLocalMscorlib>` to the test project.
* Update relevent exclusion lists:
  * Note that there are two build pipelines Jenkin's CI and nightly Helix - both must be updated for expected exclusion.
    * Jenkin's CI build - see the associated `*.txt` files under `tests/` (e.g. `tests/testsUnsupportedOutsideWindows.txt`).
    * Official nightly Helix build - see the `tests/issues.targets` file.
  * ARM/ARM64
    * `tests/arm/Tests.lst` and `tests/arm64/Tests.lst` are used to define the tests to run due to limitations with XUnit.
    * These files can be manually edited or the generated using `tests/scripts/lst_creator.py`.

### Creating a C# test project

1. Use an existing test such as `<repo_root>\tests\src\Exceptions\Finalization\Finalizer.csproj` as a template and copy it to a new folder under `<repo_root>\tests\src`.
1. Be sure that the `<AssemblyName>` property has been removed

    * Not removing this can cause confusion with the way tests are generally handled behind the scenes by the build system.

1. Set the `<CLRTestKind>`/`<CLRTestPriority>` properties.
1. Add source files to new project.
1. Indicate the success of the test by returning `100`. Failure can be indicated by any non-`100` value.

    Example:
    ```
        static public int Main(string[] notUsed)
        {
            try
            {
                // Test scenario here
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e.Message}");
                return 101;
            }

            return 100;
        }
    ```

1. Add any other projects as a dependency, if needed.
    * Managed reference: `<ProjectReference Include="../ManagedDll.csproj" />`
    * Native reference: `<ProjectReference Include="../NativeDll/CMakeLists.txt" />`
1. Build the test.
1. Follow the steps to re-run a failed test to validate the new test.
