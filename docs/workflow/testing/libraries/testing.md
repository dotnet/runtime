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

Doing full build and test runs takes a long time and is very inefficient if you need to iterate on a change. For greater control and efficiency individual parts of the build + testing workflow can be run in isolation. See the [Building instructions](../../building/libraries/README.md) for more info on build options.

### Test Run Pre-requisites

Before any tests can run we need a complete build to run them on. This requires building (1) a runtime, and (2) all the libraries. Examples:

- Build release clr + debug libraries

```
build.cmd/sh -subset clr+libs -rc Release
```

- Build release mono + debug libraries

```
build.cmd/sh -subset mono+libs -rc Release
```

Building the `libs` subset or any of individual library projects automatically copies product binaries into the testhost folder in the bin directory. This is where the tests will load the binaries from during the run. However System.Private.CorLib is an exception - the build does not automatically copy it to the testhost folder. If you [rebuild System.Private.CoreLib](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md#iterating-on-systemprivatecorelib-changes) you must also build the `libs.pretest` subset to ensure S.P.C is copied before running tests.

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

**NOTE**: if your environment doesn't have the required SDK installed (e.g. inside [Docker container](/docs/workflow/building/coreclr/linux-instructions.md#build-using-docker)),
use `./dotnet.sh`/`.\dotnet.cmd` instead of `dotnet`.

### Running only certain tests

It is possible to pass parameters to the underlying xunit runner via the `XUnitOptions` parameter, e.g., to filter to tests in just one fixture (class):

```cmd
dotnet build /t:Test /p:XUnitOptions="-class Test.ClassUnderTests"
```

or to just one test method:

```cmd
dotnet build /t:test /p:outerloop=true /p:xunitoptions="-method System.Text.RegularExpressions.Tests.RegexMatchTests.StressTestDeepNestingOfLoops"
```

### Running only specific architectures

To run tests as `x86` on a `x64` machine:

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

Each test project can potentially have multiple target frameworks. There are some tests that might be OS-specific, or might be testing an API that is available only on some target frameworks, so the `TargetFrameworks` property specifies the valid target frameworks.

### Running tests in custom compilation modes

There are several custom compilation modes for tests. These are enabled by setting a switch during the configuration. These switches are described in the following table:

| Mode           | Description                                               | Prerequisite Subsets |
| -------------- | --------------------------------------------------------- | -------------------- |
| TestSingleFile | Test using the single file compilation mode               | libs+clr             |
| TestNativeAot  | Test by compiling using NativeAOT                         | libs+clr.aot         |
| TestReadyToRun | Test compilation of the tests/libraries into R2R binaries | libs+clr             |

To run a test in a specific mode, simply build the tests after building the prerequisite subsets, and specify the test mode in the command-line. For example, to use the _TestReadyToRun_ mode in Release configuration:

```bash
dotnet build -c Release -t:Test -p:TestReadyToRun=true
```

<!-- NOTE: It might be worth it to explain what each of these flags actually does. -->
It is important to highlight that these tests do not use the standard XUnit test runner. Instead, they run with the [SingleFileTestRunner](/src/libraries/Common/tests/SingleFileTestRunner/SingleFileTestRunner.cs). The set of available commands is listed here:

- `-xml`
- `-notrait`
- `-class`
- `-class-`
- `-noclass`
- `-method`
- `-method-`
- `-nomethod`
- `-namespace`
- `-namespace-`
- `-nonamespace`
- `-parallel`

### Speeding up inner loop

A couple of flags that are sometimes helpful when iterating on a test project in the shell:

- `/p:testnobuild=true`  -- modifies `/t:test` so that it doesn't do a build before running the tests. Useful if you didn't change any code and you don't want to even check timestamps.
- `--no-restore` -- modifies `dotnet build` so that it doesn't attempt to restore packages. Useful if you're already up to date with NuGet packages.

Together these can cut a couple seconds off when you're iterating.

Putting these together, here's an example of running a single test method in a particular test project, with those flags applied:

```cmd
# assuming we're in src\libraries\System.Text.RegularExpressions
dotnet build --no-restore /t:test /p:testnobuild=true /p:xunitoptions=" -method System.Text.RegularExpressions.Tests.RegexMatchTests.Match" tests\FunctionalTests
```

If you change code, you'd need to remove `/p:testnobuild=true` from the command above.

### Viewing XUnit logs

It's usually sufficient to see the test failure output in the console. There is also a test log file, which you can find in a location like `...\runtime\artifacts\bin\System.Text.RegularExpressions.Tests\Debug\net8.0\testResults.xml`. It can be helpful, for example, to grep through a series of failures, or to see how long a slow test actually took.
