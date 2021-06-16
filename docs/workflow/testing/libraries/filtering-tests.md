# Filtering libraries tests using traits

The tests can be filtered based on xunit trait attributes defined in [`Microsoft.DotNet.XUnitExtensions`](https://github.com/dotnet/arcade/tree/master/src/Microsoft.DotNet.XUnitExtensions). These attributes are specified above the test method's definition. The available attributes are:

#### OuterLoopAttribute

```cs
[OuterLoop()]
```
Tests marked as `OuterLoop` are for scenarios that don't need to run every build. They may take longer than normal tests, cover seldom hit code paths, or require special setup or resources to execute. These tests are excluded by default when testing through `dotnet build` but can be enabled manually by adding the `-testscope outerloop` switch or `/p:TestScope=outerloop` e.g.

```cmd
build -test -testscope outerloop
cd src/System.Text.RegularExpressions/tests && dotnet build /t:Test /p:TestScope=outerloop
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

When running tests by building a test project, tests that don't apply to the `TargetOS` are not run. For example, to run Linux-specific tests on a Linux box, use the following command line:
```sh
dotnet build <csproj_file> /t:Test /p:TargetOS=Linux
```
To run all Linux-compatible tests that are failing:
```sh
dotnet build <csproj_file> /t:Test /p:TargetOS=Linux /p:WithCategories=failing
```

#### ActiveIssueAttribute
This attribute is intended to be used when there is an active issue tracking the test failure and it is needed to be fixed. This is a temporary attribute to skip the test while the issue is fixed. It is important that you limit the scope of the attribute to just the platforms and target monikers where the issue applies.

This attribute can be applied either to a test class (will disable all the tests in that class) or to a test method. It allows multiple usages on the same member.

This attribute returns the 'failing' category, which is disabled by default.

**Disable for all platforms and all target frameworks:**
```cs
[ActiveIssue(string issue)]
```
**Disable for specific platform:**
```cs
[ActiveIssue(string issue, TestPlatforms platforms)]
```
**Disable for specific runtime:**
```cs
[ActiveIssue(string issue, TestRuntimes runtimes)]
```
**Disable for specific target frameworks:**
```cs
[ActiveIssue(string issue, TargetFrameworkMonikers frameworks)]
```
**Disable for specific test platforms and target frameworks:**
```cs
[ActiveIssue(string issue, TestPlatforms platforms, TargetFrameworkMonikers frameworks)]
```
**Disable using PlatformDetection filter:**
```cs
[ActiveIssue(string issue, typeof(PlatformDetection), nameof(PlatformDetection.{member name}))]
```
Use this attribute over test methods to skip failing tests only on the specific platforms and the specific target frameworks.

#### SkipOnPlatformAttribute
This attribute is intended to disable a test permanently on a platform where an API is not available or there is an intentional difference in behavior in between the tested platform and the skipped platform.

This attribute can be applied either to a test assembly/class (will disable all the tests in that assembly/class) or to a test method. It allows multiple usages on the same member.

```cs
[SkipOnPlatform(TestPlatforms platforms, string reason)]
```

Use this attribute over test methods to skip tests only on the specific target platforms. The reason parameter doesn't affect the traits but we rather always use it so that when we see this attribute we know why it is being skipped on that platform.

If it needs to be skipped in multiple platforms and the reasons are different please use two attributes on the same test so that you can specify different reasons for each platform.

When you add the attribute on the whole test assembly it's a good idea to also add `<IgnoreForCI Condition="'$(TargetOS)' == '...'">true</IgnoreForCI>` to the test .csproj.
That allows the CI build to skip sending this test assembly to Helix completely since it'd run zero tests anyway.

**Currently these are the [Test Platforms](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/TestPlatforms.cs) that we support through our test execution infrastructure**

#### SkipOnTargetFrameworkAttribute
This attribute is intended to disable a test permanently on a framework where an API is not available or there is an intentional difference in behavior in between the tested framework and the skipped framework.

This attribute can be applied either to a test class (will disable all the tests in that class) or to a test method. It allows multiple usages on the same member.

```cs
[SkipOnTargetFramework(TargetFrameworkMonikers frameworks, string reason)]
```
Use this attribute over test methods to skip tests only on the specific target frameworks. The reason parameter doesn't affect the traits but we rather always use it so that when we see this attribute we know why it is being skipped on that framework.

If it needs to be skipped in multiple frameworks and the reasons are different please use two attributes on the same test so that you can specify different reasons for each framework.

**Currently these are the [Framework Monikers](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/TargetFrameworkMonikers.cs#L23-L26) that we support through our test execution infrastructure**

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

**Note that all of the attributes above must include an issue link and/or have a comment next to them briefly justifying the reason. ActiveIssueAttribute and SkipOnTargetFrameworkAttribute should use their constructor parameters to do this**

_**A few common examples with the above attributes:**_

- Run all tests acceptable on Windows that are not failing:
```cmd
dotnet build <csproj_file> /t:Test /p:TargetOS=windows
```
- Run all outer loop tests acceptable on OS X that are currently associated with active issues:
```sh
dotnet build <csproj_file> /t:Test /p:TargetOS=OSX /p:WithCategories="OuterLoop;failing""
```
