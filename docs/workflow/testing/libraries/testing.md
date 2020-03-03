# Testing Libraries

We use the OSS testing framework [xunit](http://xunit.github.io/).

To build the tests and run them you can call the libraries build script.

**Examples**
- The following shows how to build only the tests but not run them
```
libraries -buildtests
```

- The following builds and runs all tests for .NET Core in release configuration.
```
libraries -buildtests -test -c Release
```

- The following example shows how to pass extra msbuild properties to ignore tests ignored in CI.
```
libraries -test /p:WithoutCategories=IgnoreForCI
```

## Running tests on the command line

To build tests you need to pass the `-buildtests` flag to build.cmd/sh or if you want to build both src and tests you pass `-buildtests` flag (`libraries -restore -build -buildtests`). Note that you need to specify -restore and -build additionally as those are only implicit if no action is passed in.

If you are interested in building and running the tests only for a specific library, then there are two different ways to do it:

The easiest (and recommended) way to do it, is by simply building the test .csproj file for that library.

```cmd
cd src\libraries\System.Collections.Immutable\tests
dotnet build /t:BuildAndTest   ::or /t:Test to just run the tests if the binaries are already built
```

It is possible to pass parameters to the underlying xunit runner via the `XUnitOptions` parameter, e.g.:
```cmd
dotnet build /t:Test "/p:XUnitOptions=-class Test.ClassUnderTests"
```

There may be multiple projects in some directories so you may need to specify the path to a specific test project to get it to build and run the tests.

#### Running a single test on the command line

To quickly run or debug a single test from the command line, set the XunitMethodName property, e.g.:
```cmd
dotnet build /t:BuildAndTest /p:XunitMethodName={FullyQualifiedNamespace}.{ClassName}.{MethodName}
```

#### Running tests in a different target framework

Each test project can potentially have multiple target frameworks. There are some tests that might be OS-specific, or might be testing an API that is available only on some target frameworks, so the `TargetFrameworks` property specifies the valid target frameworks. By default we will build and run only the default build target framework which is `netcoreapp5.0`. The rest of the targetframeworks will need to be built and ran by specifying the BuildTargetFramework option.
```cmd
dotnet build src\libraries\System.Runtime\tests\System.Runtime.Tests.csproj /p:BuildTargetFramework=net472
```

## Running tests from Visual Studio

**Test Explorer** will be able to discover the tests only if the solution is opened with `build -vs` command, e.g.:
```cmd
build -vs System.Net.Http
```
If running the tests from **Test Explorer** does nothing, it probably tries to use x86 dotnet installation instead of the x64 one. It can be fixed by setting the x64 architecture manually in the test settings.

It is also possible to execute the tests by simply debugging the test project once it's been built. It will underneath call the same command as `dotnet build /t:Test` does.
