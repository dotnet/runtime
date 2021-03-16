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
    ```
* The managed portion of all tests should be able to build on any platform.
In fact in CI the managed portion of all tests will be built on OSX.
Each target will run a subset of the tests built on OSX.
Therefore the managed portion of each test **must not contain**:
  * Target platform dependent conditionally compiled code.
  * Target platform dependent conditionally included files
  * Target platform dependent conditional `<DefineConstants/>`
* Disable building and running a test on selected Targets by conditionally setting the `<CLRTestTargetUnsupported>` property.
    * e.g. `<CLRTestTargetUnsupported Condition="'$(TargetArchitecture)' == 'arm64'">true</CLRTestTargetUnsupported>`
* Disable building of a test by unconditionally setting the `<DisableProjectBuild>` property.
    * e.g. `<DisableProjectBuild>true</DisableProjectBuild>`
* Exclude test from GCStress runs by adding the following to the csproj:
    * `<GCStressIncompatible>true</GCStressIncompatible>`
* Exclude test from JIT stress runs runs by adding the following to the csproj:
    * `<JitOptimizationSensitive>true</JitOptimizationSensitive>`
* Add NuGet references by updating the following [test project](https://github.com/dotnet/runtime/blob/main/src/tests/Common/test_dependencies/test_dependencies.csproj).
* Get access to System.Private.CoreLib types and methods that are not exposed via public surface by adding the following to the csproj:
    * `<ReferenceSystemPrivateCoreLib>true</ReferenceSystemPrivateCoreLib>`
* Any System.Private.CoreLib types and methods used by tests must be available for building on all platforms.
This means there must be enough implementation for the C# compiler to find the referenced types and methods. Unsupported target platforms
should simply `throw new PlatformNotSupportedException()` in its dummy method implementations.
* Update exclusion list at [tests/issues.targets](https://github.com/dotnet/runtime/blob/main/src/tests/issues.targets) if the test fails due to active bug.

### Creating a C# test project

1. Use an existing test such as `<repo_root>\tests\src\Exceptions\Finalization\Finalizer.csproj` as a template and copy it to a new folder under `<repo_root>\tests\src`.
1. Be sure that the `<AssemblyName>` property has been removed

    * Not removing this can cause confusion with the way tests are generally handled behind the scenes by the build system.

1. Set the `<CLRTestKind>`/`<CLRTestPriority>` properties.
1. Add source files to the new project.
1. Indicate the success of the test by returning `100`. Failure can be indicated by any non-`100` value.

    Example:
    ```CSharp
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
