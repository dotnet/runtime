# Testing Libraries

## Pre-requisites
Before tests can run the following steps must happen:
1. Build a runtime (either Mono or coreclr)
2. Build the libraries
3. Testhost deployment: Binaries must be arranged into a directory layout that tools/tests expect
4. Build the tests

Using build.cmd/sh these steps can either be accomplished independently or the testing and all the pre-requisites can be automated in a single command. See the [Building instructions](../../building/libraries/README.md) for more options on how to build pieces independently.

## All-in-one Examples

These examples automate all pre-requisite steps and the test run in a single command from a clean enlistment.

- Run all tests using clr:
```
build.cmd/sh -subset clr+libs -test
```

- Run all tests using mono:
```
build.cmd/sh -subset mono+libs -test
```

- Run all tests - Builds clr in release, libs+tests in debug:
```
build.cmd/sh -subset clr+libs -test -rc Release
```

- Run all tests - Build mono and libs for x86 architecture:
```
build.cmd/sh -subset mono+libs -test -arch x86
```

## Partial workflow examples

These examples allow portions of the workflow to be isolated and done individually:

- Build only the tests but not run them:
```
build.cmd/sh -subset libs.tests
```

- Builds and run all tests in release configuration. 
_Pre-requisite: Build runtime+libs and deploy testhost directory_:
```
build.cmd/sh -subset libs.tests -test -c Release
```

- The following example shows how to pass extra msbuild properties to ignore tests ignored in CI.
_Pre-requisite: Build runtime+libs and deploy testhost directory_:
```
build.cmd/sh -subset libs.tests -test /p:WithoutCategories=IgnoreForCI
```

- Unless you specifiy `-testnobuild`, test assemblies are implicitly built when invoking the `Test` action.
The following shows how to only test the libraries without building them.
_Pre-requisite: Build runtime+libs, deploy testhost directory, build tests_:
```
build.cmd/sh -subset libs.tests -test -testnobuild
```

## Running tests on the command line

To build tests you need to specify the `test` subset when invoking build.cmd/sh: `build.cmd/sh -subset libs.tests`.

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

#### Running a single test on the command line

To quickly run or debug a single test from the command line, set the XunitMethodName property, e.g.:
```cmd
dotnet build /t:Test /p:XunitMethodName={FullyQualifiedNamespace}.{ClassName}.{MethodName}
```

#### Running outer loop tests

To run all tests, including "outer loop" tests (which are typically slower and in some test suites less reliable, but which are more comprehensive):
```cmd
dotnet build /t:Test /p:Outerloop=true
```

#### Running tests on a different target framework

Each test project can potentially have multiple target frameworks. There are some tests that might be OS-specific, or might be testing an API that is available only on some target frameworks, so the `TargetFrameworks` property specifies the valid target frameworks. By default we will build and run only the default build target framework which is `net5.0`. The rest of the `TargetFrameworks` will need to be built and ran by specifying the `BuildTargetFramework` option, e.g.:
```cmd
dotnet build src\libraries\System.Runtime\tests\System.Runtime.Tests.csproj /p:BuildTargetFramework=net472
```
