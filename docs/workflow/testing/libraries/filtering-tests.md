# Filtering libraries tests using traits

The tests can be filtered based on xunit trait attributes defined in [`Microsoft.DotNet.XUnitExtensions`](https://github.com/dotnet/arcade/tree/master/src/Microsoft.DotNet.XUnitExtensions).

Some of the attributes take arguments to restrict filtering to a subset of configurations, including platform, runtime, and target framework moniker:

`TestPlatforms` is defined [here](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/TestPlatforms.cs).

`TestRuntimes` (CoreCLR, Mono) is defined [here](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XUnitExtensions/src/TestRuntimes.cs).

`TargetFrameworkMonikers` (Netcoreapp, NetFramework) is defined [here](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XUnitExtensions/src/TargetFrameworkMonikers.cs).

These attributes are specified above the test method's definition. The available attributes are:

## OuterLoopAttribute

```cs
[OuterLoop()]
```
Tests marked as `OuterLoop` are for scenarios that don't need to run every build. They may take longer than normal tests, cover seldom hit code paths, or require special setup or resources to execute. These tests are excluded by default when testing through `dotnet build` but can be enabled manually by adding the `-testscope outerloop` switch or `/p:TestScope=outerloop` e.g.

```cmd
build -test -testscope outerloop
cd src/System.Text.RegularExpressions/tests && dotnet build /t:Test /p:TestScope=outerloop
```

This attribute is defined [here](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XUnitExtensions/src/Attributes/OuterLoopAttribute.cs).

## PlatformSpecificAttribute

```cs
[PlatformSpecific(TestPlatforms platforms)]
```
Use this attribute on test methods to specify that this test may only be run on the specified platforms. This attribute returns the following categories based on platform
- `nonwindowstests` for tests that don't run on Windows
- `nonlinuxtests` for tests that don't run on Linux
- `nonosxtests` for tests that don't run on OS X

**[Available Test Platforms](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/TestPlatforms.cs)**

When running tests by building a test project, tests that don't apply to the `TargetOS` are not run. For example, to run Linux-specific tests on a Linux box, use the following command line:
```sh
dotnet build <csproj_file> /t:Test /p:TargetOS=linux
```
To run all Linux-compatible tests that are failing:
```sh
dotnet build <csproj_file> /t:Test /p:TargetOS=linux /p:WithCategories=failing
```

This attribute is defined [here](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XUnitExtensions/src/Attributes/PlatformSpecificAttribute.cs).

## ActiveIssueAttribute
This attribute is intended to be used when there is an active issue tracking the test failure and the failure needs to be fixed. This is a temporary attribute to skip the test until the issue is fixed. It is important that you limit the scope of the attribute to just the platforms and target monikers where the issue applies.

This attribute can be applied either to a test class (will disable all the tests in that class) or to a test method. It allows multiple usages on the same member.

This attribute returns the 'failing' category, which is disabled by default.

This attribute is defined [here](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XUnitExtensions/src/Attributes/ActiveIssueAttribute.cs).

**Disable for all platforms and all target frameworks:**

```cs
[ActiveIssue(string issue)]
```
Example:
```cs
[ActiveIssue("https://github.com/dotnet/runtime/issues/17845")]
```

**Disable for specific platform:**

```cs
[ActiveIssue(string issue, TestPlatforms platforms)]
```
Examples:
```cs
[ActiveIssue("https://github.com/dotnet/runtime/issues/67853", TestPlatforms.tvOS)]
[ActiveIssue("https://github.com/dotnet/runtime/issues/52072", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
```

**Disable for specific runtime:**

```cs
[ActiveIssue(string issue, TestRuntimes runtimes)]
```
Example:
```cs
[ActiveIssue("https://github.com/dotnet/runtime/issues/2337", TestRuntimes.Mono)]
```

**Disable for specific target frameworks:**

```cs
[ActiveIssue(string issue, TargetFrameworkMonikers frameworks)]
```
Example:
```cs
[ActiveIssue("https://github.com/dotnet/runtime/issues/26624", TargetFrameworkMonikers.Netcoreapp)]
```

**Disable for specific test platforms and target frameworks:**

```cs
[ActiveIssue(string issue, TestPlatforms platforms, TargetFrameworkMonikers frameworks)]
```

**Disable using PlatformDetection filter:**

```cs
[ActiveIssue(string issue, typeof(PlatformDetection), nameof(PlatformDetection.{member name}))]
```
Example:
```cs
[ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
```

Use this attribute over test methods to skip failing tests only on the specific platforms and the specific target frameworks.

## SkipOnPlatformAttribute
This attribute is intended to disable a test permanently on a platform where an API is not available or there is an intentional difference in behavior in between the tested platform and the skipped platform.

This attribute can be applied either to a test assembly/class (will disable all the tests in that assembly/class) or to a test method. It allows multiple usages on the same member.

```cs
[SkipOnPlatform(TestPlatforms platforms, string reason)]
```
Example:
```cs
[SkipOnPlatform(TestPlatforms.Browser, "Credentials is not supported on Browser")]
```

Use this attribute over test methods to skip tests only on the specific target platforms. The reason parameter doesn't affect the traits but we rather always use it so that when we see this attribute we know why it is being skipped on that platform.

If it needs to be skipped in multiple platforms and the reasons are different please use two attributes on the same test so that you can specify different reasons for each platform.

When you add the attribute on the whole test assembly it's a good idea to also add `<IgnoreForCI Condition="'$(TargetOS)' == '...'">true</IgnoreForCI>` to the test .csproj.
That allows the CI build to skip sending this test assembly to Helix completely since it'd run zero tests anyway.

**Currently these are the [Test Platforms](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/TestPlatforms.cs) that we support through our test execution infrastructure**

## SkipOnTargetFrameworkAttribute
This attribute is intended to disable a test permanently on a framework where an API is not available or there is an intentional difference in behavior in between the tested framework and the skipped framework.

This attribute can be applied either to a test class (will disable all the tests in that class) or to a test method. It allows multiple usages on the same member.

```cs
[SkipOnTargetFramework(TargetFrameworkMonikers frameworks, string reason)]
```
Example:
```cs
[SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws a NullReferenceException")]
```

Use this attribute over test methods to skip tests only on the specific target frameworks. The reason parameter doesn't affect the traits but we rather always use it so that when we see this attribute we know why it is being skipped on that framework.

If it needs to be skipped in multiple frameworks and the reasons are different please use two attributes on the same test so that you can specify different reasons for each framework.

**Currently these are the [Framework Monikers](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/TargetFrameworkMonikers.cs#L23-L26) that we support through our test execution infrastructure**

## ConditionalFactAttribute
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

## ConditionalTheoryAttribute
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
    [MemberData(nameof(Subtract_TestData))]
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

**Note that all of the attributes above must include an issue link and/or have a comment next to them briefly justifying the reason. ActiveIssueAttribute and SkipOnTargetFrameworkAttribute should use their constructor parameters to do this**

_**A few common examples with the above attributes:**_

- Run all tests acceptable on Windows that are not failing:
```cmd
dotnet build <csproj_file> /t:Test /p:TargetOS=windows
```
- Run all outer loop tests acceptable on OS X that are currently associated with active issues:
```sh
dotnet build <csproj_file> /t:Test /p:TargetOS=osx /p:WithCategories="OuterLoop;failing""
```

## SkipOnCoreClrAttribute
This attribute is used to disable a test under specific conditions, only when run with CoreCLR (it doesn't affect tests run with Mono). Typically, this is when there is a failure in a particular test run configuration, such as under GCStress or JitStress.

This attribute can be applied either to a test class (will disable all the tests in that class) or to a test method. It allows multiple usages on the same member.

This attribute is defined [here](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XUnitExtensions/src/Attributes/SkipOnCoreClrAttribute.cs).

**Disable for all platforms and all target frameworks:**

```cs
[SkipOnCoreClr(string reason)]
```
Example:
```cs
[SkipOnCoreClr("CoreCLR does track thread specific JIT information")]
```

**Disable for specific platform:**

```cs
[SkipOnCoreClr(string reason, TestPlatforms testPlatforms)]
```
Example:
```cs
[SkipOnCoreClr("Long running tests: https://github.com/dotnet/runtime/issues/11980", TestPlatforms.Linux)
```

**Disable for specific test mode:**

A test mode is a run configuration, like JitStress, JitStressRegs, JitMinOpts, TailcallStress, GCStress.
`RuntimeTestModes` is defined [here](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XUnitExtensions/src/RuntimeTestModes.cs).

```cs
[SkipOnCoreClr(string reason, RuntimeTestModes testMode)]
```
Examples:
```cs
[SkipOnCoreClr("https://github.com/dotnet/runtime/issues/60240", RuntimeTestModes.JitStressRegs)]
[SkipOnCoreClr("Long running tests: https://github.com/dotnet/runtime/issues/10680", RuntimeTestModes.JitMinOpts)]
```

**Disable for specific runtime configuration:**

A runtime configuration is a the build of the runtime: Debug, Checked, Release.
`RuntimeConfiguration` is defined [here](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XUnitExtensions/src/RuntimeConfiguration.cs).

```cs
[SkipOnCoreClr(string reason, RuntimeConfiguration runtimeConfigurations)]
```
Example:
```cs
[SkipOnCoreClr("https://github.com/dotnet/runtime/issues/45464", ~RuntimeConfiguration.Release)]
```

**Disable for combinations of options:**

There are additional attribute signatures for combinations of these configurations, where all the conditions must be met:
```cs
SkipOnCoreClr(string reason, RuntimeConfiguration runtimeConfigurations, RuntimeTestModes testModes)
SkipOnCoreClr(string reason, TestPlatforms testPlatforms, RuntimeConfiguration runtimeConfigurations)
SkipOnCoreClr(string reason, TestPlatforms testPlatforms, RuntimeTestModes testMode)
SkipOnCoreClr(string reason, TestPlatforms testPlatforms, RuntimeConfiguration runtimeConfigurations, RuntimeTestModes testModes)
```

**Disable using multiple attributes:**

This attribute can be used multiple times, in which case the test is disabled for any of the conditions. In this example,
only Release builds where `DOTNET_JITMinOpts` is not set would run the test.
```cs
[SkipOnCoreClr("https://github.com/dotnet/runtime/issues/67886", ~RuntimeConfiguration.Release)]
[SkipOnCoreClr("https://github.com/dotnet/runtime/issues/67886", RuntimeTestModes.JitMinOpts)]
```

## SkipOnMonoAttribute

This attribute is used to disable a test only when run with Mono.

This attribute can be applied either to an assembly, class, or method.

This attribute is defined [here](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XUnitExtensions/src/Attributes/SkipOnMonoAttribute.cs).

**Disable for all platforms:**

```cs
[SkipOnMonoAttribute(string reason, TestPlatforms testPlatforms = TestPlatforms.Any)]
```
Example:
```cs
[SkipOnMono("No SAPI on Mono")]
```

## CollectionAttribute

This is a standard xunit attribute, defined [here](https://github.com/xunit/xunit/blob/07663749ab0f62597acc5ff5f163df9f5a0ab8d5/src/xunit.v3.core/CollectionAttribute.cs).

A common usage in the libraries tests is the following:

```cs
[Collection(nameof(DisableParallelization))]
```

This is put on test classes to indicate that none of the tests in that class (which as usual run serially with respect to each other) may run concurrently with tests in another class. This is used for tests that use a lot of disk space or memory, or dominate all the cores, such that they are likely to disrupt any tests that run concurrently.

## FactAttribute and `Skip`

Another way to disable the test entirely is to use the `Skip` named argument that is used on the `FactAttribute`.

Example:
```cs
[Fact(Skip = "<reason for skipping>")]
```

If the reason for skipping is a link to an issue, it is recommended to use the `ActiveIssueAttribute` instead.
Otherwise, `Skip` allows for a more descriptive reason.
