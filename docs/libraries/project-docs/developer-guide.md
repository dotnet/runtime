Developer Guide
===============

The repo can be built for the following platforms, using the provided setup and the following instructions.

| Chip  | Windows  | Linux    | macOS    | FreeBSD  |
| :---- | :------: | :------: | :------: | :------: |
| x64   | &#x2714; | &#x2714; | &#x2714; | &#x2714; |
| x86   | &#x2714; |          |          |          |
| ARM   | &#x2714; | &#x2714; |          |          |
| ARM64 | &#x2714; | &#x2714; |          |          |
|       | [Instructions](../building/windows-instructions.md) | [Instructions](../building/unix-instructions.md) | [Instructions](../building/unix-instructions.md) | [Instructions](../building/unix-instructions.md) |


Building the repository
=======================

The CoreFX repo can be built from a regular, non-admin command prompt. The build produces multiple binaries that make up the CoreFX libraries and the accompanying tests.

For information about the different options that are available use the argument `-help|-h` when calling the build script:
```
build -h
```
On Unix, arguments can be passed in with a single `-` or double hyphen `--`.

### Build
The CoreFX build has two logical components, the native build which produces the "shims" (which provide a stable interface between the OS and managed code) and
the managed build which produces the MSIL code and NuGet packages that make up CoreFX.

Calling the script `build` attempts to build both the native and managed code.

The build configurations are generally defaulted based on where you are building (i.e. which OS or which architecture) but we have a few shortcuts for the individual properties that can be passed to the build scripts:

- `-framework|-f` identifies the target framework for the build. It defaults to `netcoreapp` but possible values include `netcoreapp`, `netfx` or `uap`. (msbuild property `TargetGroup`)
- `-os` identifies the OS for the build. It defaults to the OS you are running on but possible values include `Windows_NT`, `Unix`, `Linux`, or `OSX`. (msbuild property `OSGroup`)
- `-configuration|-c Debug|Release` controls the optimization level the compilers use for the build. It defaults to `Debug`. (msbuild property `ConfigurationGroup`)
- `-arch` identifies the architecture for the build. It defaults to `x64` but possible values include `x64`, `x86`, `arm`, or `arm64`. (msbuild property `ArchGroup`)

For more details on the build configurations see [project-guidelines](../coding-guidelines/project-guidelines.md#build-pivots).

**Note**: Before working on individual projects or test projects you **must** run `build` from the root once before beginning that work. It is also a good idea to run `build` whenever you pull a large set of unknown changes into your branch. If you invoke the build script without any actions, the default action chain `-restore -build` is executed. This means that restore and build are not implicit when invoking other actions!

**Note:** You can chain multiple actions together but the order of execution is fixed and does not relate to the position of the argument in the command.

The most common workflow for developers is to call `build` from the root once and then go and work on the individual library that you are trying to make changes for.

By default build only builds the product libraries and none of the tests. If you want to build the tests you can call `build -buildtests`. If you want to run the tests you can call `build -test`. To build and run the tests combine both arguments: `build -buildtests -test`. To build both the product libraries and the test libraries pass `build -build -buildtests` to the command line.

If you invoke the build script without any argument the default arguments will be executed `-restore -build`. Note that -restore and -build are only implicit if no actions are passed in.

**Examples**
- Building in release mode for platform x64 (restore and build are implicit here as no actions are passed in)
```
build -c Release -arch x64
```

- Building the src assemblies and build and run tests (running all tests takes a considerable amount of time!)
```
build -restore -build -buildtests -test
```

- Building for different target frameworks (restore and build are implicit again as no action is passed in)
```
build -framework netcoreapp
build -framework netfx
build -framework uap
```

- Build only managed components and skip the native build
```
build /p:BuildNative=false
```

- Clean the entire solution
```
build -clean
```
### Build Native
The native build produces shims over libc, openssl, gssapi, libcurl and libz.
The build system uses CMake (2.8.12 or higher) to generate Makefiles using clang (3.5 or higher).
The build also uses git for generating some version information.

The native component should be buildable on any system.

**Examples**

- Building in debug mode for platform x64
```
./src/Native/build-native debug x64
```

- The following example shows how you would do an arm cross-compile build.
```
./src/Native/build-native debug arm cross verbose
```

For more information about extra parameters take a look at the scripts `build-native` under src/Native.

### Build And Run Tests
To build the tests and run them you can call the build script passing -tests option. The same parameters you pass to build should also be passed to ensure you are building and running the tests on the same configuration you have build the product on. However to run tests on the same machine you need to ensure the machine supports the configuration you are building.

**Examples**
- The following shows how to build only the tests but not run them
```
build -buildtests
```

- The following builds and runs all tests for netcoreapp in release configuration.
```
build -buildtests -test -c Release -f netcoreapp
```

- The following example shows how to pass extra msbuild properties to ignore tests ignored in CI.
```
build -test /p:WithoutCategories=IgnoreForCI
```

### Building individual libraries

**Note**: Before working on individual projects or test projects you **must** run `build` from the root once before beginning that work. It is also a good idea to run `build` whenever you pull a large set of unknown changes into your branch.

Similar to building the entire repo with build.cmd/sh in the root you can build projects based on our directory structure by passing in the directory. We also support
shortcuts for libraries so you can omit the root src folder from the path. When given a directory we will build all projects that we find recursively under that directory.

**Examples**

- Build all projects for a given library (ex: System.Collections) including running the tests
```
build System.Collections
```
or
```
build src\System.Collections
```
or
```
cd src\System.Collections
..\..\build .
```

- Build just the tests for a library project.
```
build src\System.Collections\tests
```

- All the options listed above like framework and configuration are also supported (note they must be after the directory)
```
build System.Collections -f netfx -c Release
```

### Building individual projects

You can either use `dotnet msbuild` or `msbuild`, depending on which is in your path. As `dotnet msbuild` works on all supported environments (i.e. Unix) we will use it throughout this guide.

**Note**: Before working on individual projects or test projects you **must** run `build` from the root once before beginning that work. It is also a good idea to run `build` whenever you pull a large set of unknown changes into your branch.

Under the src directory is a set of directories, each of which represents a particular assembly in CoreFX. See Library Project Guidelines section under [project-guidelines](../coding-guidelines/project-guidelines.md) for more details about the structure.

For example the src\System.Diagnostics.DiagnosticSource directory holds the source code for the System.Diagnostics.DiagnosticSource.dll assembly.

You can build the DLL for System.Diagnostics.DiagnosticSource.dll by going to the `src\System.Diagnostics.DiagnosticsSource\src` directory and typing `dotnet msbuild`. The DLL ends up in `bin\AnyOS.AnyCPU.Debug\System.Diagnostics.DiagnosticSource` as well as `bin\runtime\[BuildConfiguration]`.

You can build the tests for System.Diagnostics.DiagnosticSource.dll by going to
`src\System.Diagnostics.DiagnosticSource\tests` and typing `dotnet msbuild`.

Some libraries might also have a ref and/or a pkg directory and you can build them in a similar way by typing `dotnet msbuild` in that directory.

For libraries that have multiple build configurations the configurations will be listed in the `<BuildConfigurations>` property group, commonly found in a configurations.props file next to the csproj. When building the csproj for a configuration the most compatible one in the list will be chosen and set for the build. For more information about `BuildConfigurations` see [project-guidelines](../coding-guidelines/project-guidelines.md).

**Examples**

- Build project for Linux for netcoreapp
```
dotnet msbuild System.Net.NetworkInformation.csproj /p:OSGroup=Linux
```

- Build project for uap (not if trying to build on non-windows you also need to specify OSGroup=Windows_NT)
```
dotnet msbuild System.Net.NetworkInformation.csproj /p:TargetGroup=uap
```

- Build release version of library
```
dotnet msbuild System.Net.NetworkInformation.csproj /p:ConfigurationGroup=Release
```

To build for all supported configurations you can use the `BuildAll` and `RebuildAll` tasks:

```
dotnet msbuild System.Net.NetworkInformation.csproj /t:RebuildAll
```

### Building all for other OSes

By default, building from the root will only build the libraries for the OS you are running on. One can
build for another OS by specifying `build -os [value]`.

Note that you cannot generally build native components for another OS but you can for managed components so if you need to do that you can do it at the individual project level or build all via passing `/p:BuildNative=false`.

### Building in Release or Debug

By default, building from the root or within a project will build the libraries in Debug mode.
One can build in Debug or Release mode from the root by doing `build -c Release` or `build -c Debug` or when building a project by specifying `/p:ConfigurationGroup=[Debug|Release]` after the `dotnet msbuild` command.

### Building other Architectures

One can build 32- or 64-bit binaries or for any architecture by specifying in the root `build -arch [value]` or in a project `/p:ArchGroup=[value]` after the `dotnet msbuild` command.

### Benchmarks

All Benchmarks have been moved to the [dotnet/performance/](https://github.com/dotnet/performance/) repository.

Please read the [Benchmarking workflow for CoreFX](https://github.com/dotnet/performance/blob/master/docs/benchmarking-workflow-corefx.md) document to find out how to build and run the Benchmarks.

### Tests

We use the OSS testing framework [xunit](http://xunit.github.io/).

#### Running tests on the command line

To build tests you need to pass the `-buildtests` flag to build.cmd/sh or if you want to build both src and tests you pass `-buildtests` flag (`build -restore -build -buildtests`). Note that you need to specify -restore and -build additionally as those are only implicit if no action is passed in.

For more information about cross-platform testing, please take a look [here](https://github.com/dotnet/corefx/blob/master/Documentation/building/cross-platform-testing.md).

If you are interested in building and running the tests only for a specific library, then there are two different ways to do it:

The easiest (and recommended) way to do it, is by simply building the test .csproj file for that library.

```cmd
cd src\System.Collections.Immutable\tests
dotnet msbuild /t:BuildAndTest   ::or /t:Test to just run the tests if the binaries are already built
dotnet msbuild /t:RebuildAndTest ::this will cause a test project to rebuild and then run tests
```

It is possible to pass parameters to the underlying xunit runner via the `XunitOptions` parameter, e.g.:
```cmd
dotnet msbuild /t:Test "/p:XunitOptions=-class Test.ClassUnderTests"
```

There may be multiple projects in some directories so you may need to specify the path to a specific test project to get it to build and run the tests.

Tests participate in the incremental build.  This means that if tests have already been run, and inputs to the incremental build have not changed, rerunning the tests target will not execute the test runner again.  To force re-executing tests in this situation, use `/p:ForceRunTests=true`.

#### Running a single test on the command line

To quickly run or debug a single test from the command line, set the XunitMethodName property (found in Tools\tests.targets) to the full method name (including namespace), e.g.:
```cmd
dotnet msbuild /t:RebuildAndTest /p:XunitMethodName={FullyQualifiedNamespace}.{ClassName}.{MethodName}
```

#### Running tests in a different target framework

Each test project can potentially have multiple build configurations. There are some tests that might be OS-specific, or might be testing an API that is available only on some target frameworks, so the `BuildConfigurations` property specifies the valid configurations. By default we will build and run only the default build configuration which is `netcoreapp`. The rest of the configurations will need to be built and ran by specifying the configuration options.

```cmd
cd src\System.Runtime\tests
dotnet msbuild System.Runtime.Tests.csproj /p:TargetGroup=netfx
```

#### Filtering tests using traits

The tests can also be filtered based on xunit trait attributes defined in [`Microsoft.DotNet.XUnitExtensions`](https://github.com/dotnet/arcade/tree/master/src/Microsoft.DotNet.XUnitExtensions). These attributes are specified above the test method's definition. The available attributes are:

#### OuterLoopAttribute

```cs
[OuterLoop()]
```
Tests marked as `OuterLoop` are for scenarios that don't need to run every build. They may take longer than normal tests, cover seldom hit code paths, or require special setup or resources to execute. These tests are excluded by default when testing through `dotnet msbuild` but can be enabled manually by adding the `-testscope outerloop` switch or `/p:TestScope=outerloop` e.g.

```cmd
build -test -testscope outerloop
cd src/System.Text.RegularExpressions/tests && dotnet msbuild /t:RebuildAndTest /p:TestScope=outerloop
```

#### PlatformSpecificAttribute

```cs
[PlatformSpecific(TestPlatforms platforms)]
```
Use this attribute on test methods to specify that this test may only be run on the specified platforms. This attribute returns the following categories based on platform
- `nonwindowstests` for tests that don't run on Windows
- `nonlinuxtests` for tests that don't run on Linux
- `nonosxtests` for tests that don't run on OS X

**[Available Test Platforms](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/TestPlatforms.cs)**

When running tests by building a test project, tests that don't apply to the `OSGroup` are not run. For example, to run Linux-specific tests on a Linux box, use the following command line:
```sh
dotnet msbuild <csproj_file> /t:BuildAndTest /p:OSGroup=Linux
```
To run all Linux-compatible tests that are failing:
```sh
dotnet msbuild <csproj_file> /t:BuildAndTest /p:OSGroup=Linux /p:WithCategories=failing
```

#### ActiveIssueAttribute
This attribute is intended to be used when there is an active issue tracking the test failure and it is needed to be fixed. This is a temporary attribute to skip the test while the issue is fixed. It is important that you limit the scope of the attribute to just the platforms and target monikers where the issue applies.

This attribute can be applied either to a test class (will disable all the tests in that class) or to a test method. It allows multiple usages on the same member.

This attribute returns the 'failing' category, which is disabled by default.

**Disable for all platforms and all target frameworks:**
```cs
[ActiveIssue(int issue)]
[ActiveIssue(string issue)]
```
**Disable for specific platform:**
```cs
[ActiveIssue(int issue, TestPlatforms platforms)]
[ActiveIssue(string issue, TestPlatforms platforms)]
```
**Disable for specific target frameworks:**
```cs
[ActiveIssue(int issue, TargetFrameworkMonikers frameworks)]
[ActiveIssue(string issue, TargetFrameworkMonikers frameworks)]
```
**Disable for specific test platforms and target frameworks:**
```cs
[ActiveIssue(int issue, TestPlatforms platforms, TargetFrameworkMonikers frameworks)]
[ActiveIssue(string issue, TestPlatforms platforms, TargetFrameworkMonikers frameworks)]
```
Use this attribute over test methods to skip failing tests only on the specific platforms and the specific target frameworks.

#### SkipOnTargetFrameworkAttribute
This attribute is intended to disable a test permanently on a framework where an API is not available or there is an intentional difference in behavior in between the tested framework and the skipped framework.

This attribute can be applied either to a test class (will disable all the tests in that class) or to a test method. It allows multiple usages on the same member.

```cs
[SkipOnTargetFramework(TargetFrameworkMonikers frameworks, string reason)]
```
Use this attribute over test methods to skip tests only on the specific target frameworks. The reason parameter doesn't affect the traits but we rather always use it so that when we see this attribute we know why it is being skipped on that framework.

If it needs to be skipped in multiple frameworks and the reasons are different please use two attributes on the same test so that you can specify different reasons for each framework.

**Currently this are the [Framework Monikers](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/TargetFrameworkMonikers.cs#L23-L26) that we support through our test execution infrastructure**

#### ConditionalFactAttribute
Use this attribute to run the test only when a condition is `true`. This attribute is used when `ActiveIssueAttribute` or `SkipOnTargetFrameworkAttribute` are not flexible enough due to needing to run a custom logic at test time. This test behaves as a `[Fact]` test that has no test data passed in as a parameter.

```cs
[ConditionalFact(params string[] conditionMemberNames)]
```

The conditional method needs to be a static method or property on this or any ancestor type, of any visibility, accepting zero arguments, and having a return type of Boolean.

**Example:**
```cs
public class TestClass
{
    public static bool ConditionProperty => true;

    [ConditionalFact(nameof(ConditionProperty))]
    public static void TestMethod()
    {
        Assert.True(true);
    }
}
```

#### ConditionalTheoryAttribute
Use this attribute to run the test only when a condition is `true`. This attribute is used when `ActiveIssueAttribute` or `SkipOnTargetFrameworkAttribute` are not flexible enough due to needing to run a custom logic at test time. This test behaves as a `[Theory]` test that has no test data passed in as a parameter.

```cs
[ConditionalTheory(params string[] conditionMemberNames)]
```

This attribute must have `[MemberData(string member)]` or a `[ClassData(Type class)]` attribute, which represents an `IEnumerable<object>` containing the data that will be passed as a parameter to the test. Another option is to add multiple or one `[InlineData(object params[] parameters)]` attribute.

The conditional method needs to be a static method or property on this or any ancestor type, of any visibility, accepting zero arguments, and having a return type of Boolean.

**Example:**
```cs
public class TestClass
{
    public static bool ConditionProperty => true;

    public static IEnumerable<object[]> Subtract_TestData()
    {
        yield return new object[] { new IntPtr(42), 6, (long)36 };
        yield return new object[] { new IntPtr(40), 0, (long)40 };
        yield return new object[] { new IntPtr(38), -2, (long)40 };
    }

    [ConditionalTheory(nameof(ConditionProperty))]
    [MemberData(nameof(Equals_TestData))]
    public static void Subtract(IntPtr ptr, int offset, long expected)
    {
        IntPtr p1 = IntPtr.Subtract(ptr, offset);
        VerifyPointer(p1, expected);

        IntPtr p2 = ptr - offset;
        VerifyPointer(p2, expected);

        IntPtr p3 = ptr;
        p3 -= offset;
        VerifyPointer(p3, expected);
    }
}
```

**Note that all of the attributes above must include an issue number/link and/or have a comment next to them briefly justifying the reason. ActiveIssueAttribute and SkipOnTargetFrameworkAttribute should use their constructor parameters to do this**

_**A few common examples with the above attributes:**_

- Run all tests acceptable on Windows that are not failing:
```cmd
dotnet msbuild <csproj_file> /t:BuildAndTest /p:OSGroup=Windows_NT
```
- Run all outer loop tests acceptable on OS X that are currently associated with active issues:
```sh
dotnet msbuild <csproj_file> /t:BuildAndTest /p:OSGroup=OSX /p:WithCategories="OuterLoop;failing""
```

### Code Coverage

Code coverage is built into the corefx build system.  It utilizes OpenCover for generating coverage data and ReportGenerator for generating reports about that data.  To run:

```cmd
:: Run full coverage (assuming sources and tests are already built)
build -test -coverage

If coverage succeeds, the full report can be found at `artifacts\coverage\index.htm`.

:: To run a single project with code coverage enabled pass the /p:Coverage=true property
cd src\System.Collections.Immutable\tests
dotnet msbuild /t:BuildAndTest /p:Coverage=true
```

If coverage succeeds, the individual report can be found at `$(TestPath)\report\index.htm`.

Code coverage reports from the continuous integration system are available from the links on the front page of the corefx repo.

### Building tests with UWP (Windows only)

This will allow you to build and run against `uap`, the managed version of the UWP Framework subset, used when debugging UWP applications in Visual Studio:
```cmd
cd src\Microsoft.CSharp\tests
dotnet msbuild /t:BuildAndTest /p:TargetGroup=uap
```
In this case, your test will get executed within the context of a wrapper UWP application, targeting the Managed uap.

The CoreFX build and test suite is a work in progress, as are the [building and testing instructions](../README.md). The .NET Core team and the community are improving Linux and OS X support on a daily basis and are adding more tests for all platforms. See [CoreFX Issues](https://github.com/dotnet/corefx/issues) to find out about specific work items or report issues.

## Testing with private CoreCLR bits

Generally the CoreFx build system gets the CoreCLR from a NuGet package which gets pulled down and correctly copied to the various output directories by building '\external\runtime\runtime.depproj' which gets built as part of `build.cmd/sh`. For folks that want to do builds and test runs in corefx with a local private build of coreclr you can follow these steps:


1. Build CoreCLR and note your output directory. Ex: `\coreclr\bin\Product\Windows_NT.x64.Release\` Note this will vary based on your OS/Architecture/Flavor and it is generally a good idea to use Release builds for CoreCLR when running CoreFx tests and the OS and Architecture should match what you are building in CoreFx.
2. Build CoreFx either passing in the `CoreCLROverridePath` property or setting it as an environment variable:
```
build.cmd /p:CoreCLROverridePath=d:\git\coreclr\bin\Product\Windows_NT.x64.Release\
```

When we copy the files to override the CoreCLR we do a hard-link copy, so in general if you rebuild CoreCLR the new binaries should be reflected in CoreFx for subsequent builds of individual projects in corefx. However if you want to force refresh or if you want to just update the CoreCLR after you already ran `build.cmd` from CoreFx you can run the following command:

```
dotnet msbuild /p:CoreCLROverridePath=d:\git\coreclr\bin\Product\Windows_NT.x64.Release\ ./external/runtime/runtime.depproj
```

By convention the project will look for PDBs in a directory under `$(CoreCLROverridePath)/PDB` and if found will also copy them. If not found no PDBs will be copied. If you want to explicitly set the PDB path then you can pass `CoreCLRPDBOverridePath` property to that PDB directory.

Once you have updated your CoreCLR you can run tests however you usually do (via build.cmd -test, individual test project, in VS, etc) and it should be using your copy of CoreCLR.

If you prefer, you can use a Debug build of System.Private.CoreLib, but if you do you must also use a Debug build of the native portions of the runtime, e.g. coreclr.dll. Tests with a Debug runtime will execute much more slowly than with Release runtime bits.

If the test project does not set the property `TestRuntime` to `true` and you want to collect code coverage that includes types in System.Private.CoreLib.dll, you'll need to follow the above steps, then

`dotnet msbuild /t:rebuildandtest /p:Coverage=true /p:TestRuntime=true`
