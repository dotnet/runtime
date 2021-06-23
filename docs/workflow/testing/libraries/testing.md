# Testing Libraries

## Full Build and Test Run

These example commands automate the test run and all pre-requisite build steps in a single command from a clean enlistment.

- Run all tests - Builds clr in release, libs+tests in debug:
```
build.cmd/sh -subset clr+libs+libs.tests -test -rc Release
```

- Run all tests - Builds Mono in release, libs+tests in debug:
```
build.cmd/sh -subset mono+libs+libs.tests -test -rc Release
```

- Run all tests - Build Mono and libs for x86 architecture in debug (choosing debug for runtime will run very slowly):
```
build.cmd/sh -subset mono+libs+libs.tests -test -arch x86
```

## Partial Build and Test Runs

Doing full build and test runs takes a long time and is very inefficient if you need to iterate on a change.
For greater control and efficiency individual parts of the build + testing workflow can be run in isolation.
See the [Building instructions](../../building/libraries/README.md) for more info on build options.

### Test Run Pre-requisites
Before any tests can run we need a complete build to run them on. This requires building (1) a runtime, and
(2) all the libraries. Examples:

- Build release clr + debug libraries
```
build.cmd/sh -subset clr+libs -rc Release
```

- Build release mono + debug libraries
```
build.cmd/sh -subset mono+libs -rc Release
```

Building the `libs` subset or any of individual library projects automatically copies product binaries into the testhost folder
in the bin directory. This is where the tests will load the binaries from during the run. However System.Private.CorLib is an
exception - the build does not automatically copy it to the testhost folder. If you [rebuild System.Private.CoreLib](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md#iterating-on-systemprivatecorelib-changes) you must also build the `libs.pretest` subset to ensure S.P.C is copied before running tests.

### Running tests for all libraries

- Build and run all tests in release configuration.
```
build.cmd/sh -subset libs.tests -test -c Release
```

- Build the tests without running them
```
build.cmd/sh -subset libs.tests
```

- Run the tests without building them
```
build.cmd/sh -subset libs.tests -test -testnobuild
```

- The following example shows how to pass extra msbuild properties to ignore tests ignored in CI.
```
build.cmd/sh -subset libs.tests -test /p:WithoutCategories=IgnoreForCI
```

### Running tests for a single library

The easiest (and recommended) way to build and run the tests for a specific library, is to invoke the `Test` target on that library:
```cmd
cd src\libraries\System.Collections.Immutable\tests
dotnet build /t:Test
```

It is possible to pass parameters to the underlying xunit runner via the `XUnitOptions` parameter, e.g.:
```cmd
dotnet build /t:Test /p:XUnitOptions="-class Test.ClassUnderTests"
```

Which is very useful when you want to run tests as `x86` on a `x64` machine:
```cmd
dotnet build /t:Test /p:TargetArchitecture=x86
```

There may be multiple projects in some directories so you may need to specify the path to a specific test project to get it to build and run the tests.

### Running a single test on the command line

To quickly run or debug a single test from the command line, set the XunitMethodName property, e.g.:
```cmd
dotnet build /t:Test /p:XunitMethodName={FullyQualifiedNamespace}.{ClassName}.{MethodName}
```

### Running outer loop tests

To run all tests, including "outer loop" tests (which are typically slower and in some test suites less reliable, but which are more comprehensive):
```cmd
dotnet build /t:Test /p:Outerloop=true
```

### Running tests on a different target framework

Each test project can potentially have multiple target frameworks. There are some tests that might be OS-specific, or might be testing an API that is available only on some target frameworks, so the `TargetFrameworks` property specifies the valid target frameworks. By default we will build and run only the default build target framework which is `net5.0`. The rest of the `TargetFrameworks` will need to be built and ran by specifying the `BuildTargetFramework` option, e.g.:
```cmd
dotnet build src\libraries\System.Runtime\tests\System.Runtime.Tests.csproj /p:BuildTargetFramework=net472
```
