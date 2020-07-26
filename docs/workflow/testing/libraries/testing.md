# Testing Libraries

We use the OSS testing framework [xunit](http://xunit.github.io/).

To build the tests and run them you can call the libraries build script.

**Examples**
- The following shows how to build only the tests but not run them:
```
build.cmd/sh libs.tests
```

- The following builds and runs all tests in release configuration:
```
build.cmd/sh libs.tests -test -c Release
```

- The following example shows how to pass extra msbuild properties to ignore tests ignored in CI:
```
build.cmd/sh libs.tests -test /p:WithoutCategories=IgnoreForCI
```

Unless you specifiy `-testnobuild`, test assemblies are implicitly built when invoking the `Test` action.
- The following shows how to only test the libraries without building them
```
build.cmd/sh -test -testnobuild
```

## Running tests on the command line

To build tests you need to specify the `libs.tests` subset when invoking build.cmd/sh: `build.cmd/sh libs.tests`.

The easiest (and recommended) way to build and run the tests for a specific library, is to invoke `dotnet test` on that library:
```cmd
dotnet test src\libraries\System.Collections.Immutable\tests
```

It is possible to pass parameters to the underlying VSTest runner, e.g.:
```cmd
dotnet test --filter Test.ClassUnderTests
```

The available arguments for `dotnet test` are documented [here](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test) and advanced filtering options can be found [here](https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit). 

There may be multiple projects in some directories so you may need to specify the path to a specific test project to get it to build and run the tests.

#### Running a single test on the command line

To quickly run or debug a single test from the command line, supply a filter argument value. property, e.g.:
```cmd
dotnet test --filter {FullyQualifiedNamespace}.{ClassName}.{MethodName}
```

#### Running outer loop tests

To run all tests, including "outer loop" tests (which are typically slower and in some test suites less reliable, but which are more comprehensive):
```cmd
dotnet test /p:Outerloop=true
```

#### Running tests on a different target framework

Each test project can potentially have multiple target frameworks. There are some tests that might be OS-specific, or might be testing an API that is available only on some target frameworks, so the `TargetFrameworks` property specifies the valid target frameworks. By default we will build and run only the default build target framework which is `net5.0`. The rest of the `TargetFrameworks` will need to be built and ran by specifying the `BuildTargetFramework` option, e.g.:
```cmd
dotnet test -f net48 src\libraries\System.Text.RegularExpressions\tests
```
