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
* Exclude test from HeapVerify testing runs runs by adding the following to the csproj:
    * `<HeapVerifyIncompatible>true</HeapVerifyIncompatible>`
* Exclude test from JIT stress runs runs by adding the following to the csproj:
    * `<JitOptimizationSensitive>true</JitOptimizationSensitive>`
* Exclude test from NativeAOT runs runs by adding the following to the csproj:
    * `<NativeAotIncompatible>true</NativeAotIncompatible>`
* Exclude the test from ilasm round trip testing by adding the following to the csproj
    * `<IlasmRoundTripIncompatible>true</IlasmRoundTripIncompatible>`
* Exclude the test for unloadability (collectible assemblies) testing
    * `<UnloadabilityIncompatible>true</UnloadabilityIncompatible>`
* If the test is specific for testing crossgen2, and should be compiled as such in all test modes
    * `<AlwaysUseCrossGen2>true</AlwaysUseCrossGen2>`
* When `CrossGenTest` is set to false, this test is not run with standard R2R compilation even if running an R2R test pass.
    * `<CrossGenTest>false</CrossGenTest>`
* Add NuGet references by updating the following [test project](/src/tests/Common/test_dependencies/test_dependencies.csproj).
* Any System.Private.CoreLib types and methods used by tests must be available for building on all platforms.
This means there must be enough implementation for the C# compiler to find the referenced types and methods. Unsupported target platforms
should simply `throw new PlatformNotSupportedException()` in its dummy method implementations.
* Update exclusion list at [tests/issues.targets](/src/tests/issues.targets) if the test fails due to active bug.

### Creating a C# test project

1. Use an existing test such as `<repo_root>\tests\src\Exceptions\Finalization\Finalizer.csproj` as a template and copy it to a new folder under `<repo_root>\tests\src`.
1. Be sure that the `<AssemblyName>` property has been removed

    * Not removing this can cause confusion with the way tests are generally handled behind the scenes by the build system.

1. Set the `<CLRTestKind>`/`<CLRTestPriority>` properties.
1. Add source files to the new project.
1. Add test cases using the Xunit `Fact` attribute.

    - We use a source generator to construct the `Main` entry point for test projects. The source generator will discover all methods marked with `Fact` and call them from the generated `Main`.
    - Alternatively, `Main` can be user-defined. On success, the test returns `100`. Failure can be indicated by any non-`100` value.

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
                  Console.WriteLine($"Test Failure: {e}");
                  return 101;
              }

              return 100;
          }
      ```

1. Add any other projects as a dependency, if needed.
    * Managed reference: `<ProjectReference Include="../ManagedDll.csproj" />`
    * CMake reference: `<CMakeProjectReference Include="../NativeDll/CMakeLists.txt" />`
1. Build the test.
1. Follow the steps to re-run a failed test to validate the new test.

### Creating a merged test runner project
1. Use an existing test such as `<repo_root>\src\tests\JIT\Methodical\Methodical_d1.csproj` as a template.
1. If your new merged test runner has MANY tests in it, and takes too long to run under GC Stress, set `<NumberOfStripesToUseInStress>` to a number such as 10 to make it possible for the test to complete in a reasonable timeframe.

#### Command line arguments for merged test runner projects
Unless tests are manually run on the command line to repro a problem, these parameters are handled internally by the test infrastructure, but for running tests locally, there are a set of standard parameters that these merged test runners support.

`[testFilterString] [-stripe <whichStripe> <totalStripes>]`

`testFilterString` is any string other that `-stripe`. The only filters supported today are the simple form supported in 'dotnet test --filter' (substrings of the test's fully qualified name).

Either the -stripe <whichStripe> <totalStripes> parameter can be used or the TEST_HARNESS_STRIPE_TO_EXECUTE environment variable may be used to control striping. The TEST_HARNESS_STRIPE_TO_EXECUTE environment variable must be set to a string of the form `.<whichStripe>.<totalStripes>` if it is used. `<whichStripe>` is a 0 based index into the count of stripes, `<totalStripes>` is the total number of stripes.

