// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Compile-time shim definitions for Microsoft.DotNet.XUnitV3Extensions types.
//
// When NativeAOT test projects use the xunit v4 AOT packages, the
// Microsoft.DotNet.XUnitV3Extensions package cannot be referenced because
// FactAttribute and TheoryAttribute are sealed in the AOT packages.
//
// These stubs let test source compile without the Extensions package.
// Attributes that inherit from FactAttribute/TheoryAttribute in the real
// package are plain [Attribute] stubs here — methods decorated with them
// will NOT be discovered as tests by the AOT source generator (which only
// recognizes [Fact] and [Theory]).  This is the expected trade-off:
// only plain [Fact]/[Theory] tests run in NativeAOT mode.
//
// ActiveIssueAttribute extends BeforeAfterTestAttribute so that tests
// decorated with [Fact]/[Theory] + [ActiveIssue] are skipped at runtime
// when their conditions match, rather than running and failing.
//
// ConditionalClassAttribute is intentionally NOT stubbed — see comment in the
// Xunit namespace below. Use Assert.SkipUnless in constructors instead.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit.Sdk;
using Xunit.v3;

namespace Xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ConditionalFactAttribute : Attribute
    {
        public ConditionalFactAttribute(Type conditionType, params string[] conditionMemberNames) { }
        public ConditionalFactAttribute(params Type[] conditions) { }
        public string? DisplayName { get; set; }
        public string? Skip { get; set; }
        public int Timeout { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ConditionalTheoryAttribute : Attribute
    {
        public ConditionalTheoryAttribute(Type conditionType, params string[] conditionMemberNames) { }
        public ConditionalTheoryAttribute(params Type[] conditions) { }
        public string? DisplayName { get; set; }
        public string? Skip { get; set; }
        public bool SkipTestWithoutData { get; set; }
        public int Timeout { get; set; }
    }

    // ConditionalClassAttribute is intentionally NOT stubbed here. The real
    // implementation in Microsoft.DotNet.XUnitV3Extensions works via traits in
    // non-AOT builds. For NativeAOT builds, classes should use Assert.SkipUnless
    // in the constructor instead — this works in both AOT and non-AOT modes.
    // Removing the stub ensures any new usage produces a compile error, forcing
    // an explicit fix rather than silently running tests that should be skipped.

    /// <summary>
    /// Marks a test as having a known active issue. In NativeAOT builds, this
    /// attribute extends <see cref="BeforeAfterTestAttribute"/> so that tests
    /// decorated with <c>[Fact]</c>/<c>[Theory]</c> plus <c>[ActiveIssue]</c>
    /// are skipped at runtime when the specified conditions match.
    /// </summary>
    /// <remarks>
    /// The xUnit v3 AOT source generator always instantiates BeforeAfterTestAttribute
    /// subclasses via their parameterless constructor (<c>new T()</c>), discarding
    /// all constructor arguments from metadata. To work around this, the
    /// <see cref="Before"/> method reads the original attribute metadata from the
    /// test method, class, and assembly using <see cref="CustomAttributeData"/> and
    /// evaluates the skip conditions from the actual constructor arguments.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ActiveIssueAttribute : BeforeAfterTestAttribute
    {
        // The parameterless constructor is required by the xUnit v3 AOT source generator,
        // which emits `new ActiveIssueAttribute()` in generated code. It is never used
        // directly by test authors (the real package has no parameterless constructor).
        // The remaining constructors match the real ActiveIssueAttribute API so test
        // source code compiles against this stub.

        public ActiveIssueAttribute() { }
        public ActiveIssueAttribute(string issue) { }
        public ActiveIssueAttribute(string issue, TestPlatforms platforms) { }
        public ActiveIssueAttribute(string issue, TargetFrameworkMonikers frameworks) { }
        public ActiveIssueAttribute(string issue, TestRuntimes runtimes) { }
        public ActiveIssueAttribute(string issue, TestPlatforms platforms, TestRuntimes runtimes) { }
        public ActiveIssueAttribute(string issue, TestPlatforms platforms, TargetFrameworkMonikers frameworks, TestRuntimes runtimes) { }
        public ActiveIssueAttribute(string issue, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type conditionType, params string[] conditionMemberNames) { }

        public override void Before(ICodeGenTest test)
        {
            // Read the real ActiveIssueAttribute metadata from the test method, class,
            // and assembly (since the source generator discards constructor arguments).
            foreach (var parsed in GetActiveIssueMetadata(test))
            {
                if (ShouldSkip(parsed))
                {
                    throw SkipException.ForSkip($"Active issue: {parsed.Issue}");
                }
            }
        }

        private static IEnumerable<ActiveIssueData> GetActiveIssueMetadata(ICodeGenTest test)
        {
            var testMethod = test.TestCase.TestMethod;
            var testClass = testMethod.TestClass;

            // Method-level attributes
            MethodInfo? methodInfo = ResolveMethod(testMethod, testClass);
            if (methodInfo is not null)
            {
                foreach (var data in ReadActiveIssueData(CustomAttributeData.GetCustomAttributes(methodInfo)))
                    yield return data;
            }

            // Class-level attributes (check declaring type if different from test class)
            Type classType = testClass.Class;
            foreach (var data in ReadActiveIssueData(CustomAttributeData.GetCustomAttributes(classType)))
                yield return data;

            // If the method is declared on a base type, also check that type
            if (methodInfo?.DeclaringType is not null && methodInfo.DeclaringType != classType)
            {
                foreach (var data in ReadActiveIssueData(CustomAttributeData.GetCustomAttributes(methodInfo.DeclaringType)))
                    yield return data;
            }

            // Assembly-level attributes
            foreach (var data in ReadActiveIssueData(CustomAttributeData.GetCustomAttributes(classType.Assembly)))
                yield return data;
        }

        private static MethodInfo? ResolveMethod(Xunit.Sdk.ITestMethodMetadata testMethod, Xunit.v3.ICoreTestClass testClass)
        {
            string methodName = testMethod.MethodName;
            Type classType = testClass.Class;

            // If the method is declared on a different type (base class), resolve from there
            if (testMethod is Xunit.v3.ICodeGenTestMethod codeGenMethod
                && codeGenMethod.DeclaredTypeIndex is string declaredTypeIndex)
            {
                // DeclaredTypeIndex uses "global::Namespace.Type" format; strip the prefix
                string typeName = declaredTypeIndex.StartsWith("global::", StringComparison.Ordinal)
                    ? declaredTypeIndex.Substring("global::".Length)
                    : declaredTypeIndex;
                Type? declaredType = classType.Assembly.GetType(typeName)
                    ?? Type.GetType(typeName);
                if (declaredType is not null)
                    classType = declaredType;
            }

            // Use GetMethods to handle overloads gracefully - pick the first match
            try
            {
                return classType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            }
            catch
            {
                // If reflection fails (trimmed metadata, ambiguous match, etc.), fail open
                return null;
            }
        }

        private static IEnumerable<ActiveIssueData> ReadActiveIssueData(IList<CustomAttributeData> attributes)
        {
            foreach (var attr in attributes)
            {
                if (attr.AttributeType != typeof(ActiveIssueAttribute))
                    continue;

                var args = attr.ConstructorArguments;
                if (args.Count == 0)
                    continue;

                string issue = args[0].Value as string ?? string.Empty;

                if (args.Count == 1)
                {
                    // ActiveIssueAttribute(string issue)
                    yield return new ActiveIssueData(issue, TestPlatforms.Any, TargetFrameworkMonikers.Any,
                        TestRuntimes.CoreCLR | TestRuntimes.Mono, null, []);
                }
                else if (args.Count >= 2 && args[1].ArgumentType == typeof(Type))
                {
                    // ActiveIssueAttribute(string issue, Type conditionType, params string[] conditionMemberNames)
                    Type? conditionType = args[1].Value as Type;
                    string[] memberNames = args.Count > 2
                        ? args.Skip(2).SelectMany(a =>
                            a.ArgumentType.IsArray
                                ? ((System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument>)a.Value!).Select(e => (string)e.Value!)
                                : [(string)a.Value!]).ToArray()
                        : [];
                    yield return new ActiveIssueData(issue, TestPlatforms.Any, TargetFrameworkMonikers.Any,
                        TestRuntimes.CoreCLR | TestRuntimes.Mono, conditionType, memberNames);
                }
                else
                {
                    // Enum-based overloads: determine which enums are present by argument types
                    var platforms = TestPlatforms.Any;
                    var frameworks = TargetFrameworkMonikers.Any;
                    var runtimes = TestRuntimes.CoreCLR | TestRuntimes.Mono;

                    for (int i = 1; i < args.Count; i++)
                    {
                        if (args[i].ArgumentType == typeof(TestPlatforms))
                            platforms = (TestPlatforms)(int)args[i].Value!;
                        else if (args[i].ArgumentType == typeof(TargetFrameworkMonikers))
                            frameworks = (TargetFrameworkMonikers)(int)args[i].Value!;
                        else if (args[i].ArgumentType == typeof(TestRuntimes))
                            runtimes = (TestRuntimes)(int)args[i].Value!;
                    }

                    yield return new ActiveIssueData(issue, platforms, frameworks, runtimes, null, []);
                }
            }
        }

        private static bool ShouldSkip(ActiveIssueData data)
        {
            if (data.ConditionType is not null)
            {
                return EvaluateTypeConditions(data.ConditionType, data.ConditionMemberNames);
            }

            return MatchesPlatform(data.Platforms)
                && MatchesFramework(data.Frameworks)
                && MatchesRuntime(data.Runtimes);
        }

        private static bool EvaluateTypeConditions(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type conditionType,
            string[] memberNames)
        {
            foreach (string memberName in memberNames)
            {
                if (!EvaluateMember(conditionType, memberName))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EvaluateMember(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type type,
            string memberName)
        {
            PropertyInfo? property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
            if (property?.PropertyType == typeof(bool))
            {
                return (bool)property.GetValue(null)!;
            }

            MethodInfo? method = type.GetMethod(memberName, BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            if (method?.ReturnType == typeof(bool))
            {
                return (bool)method.Invoke(null, null)!;
            }

            return false;
        }

        private static bool MatchesPlatform(TestPlatforms platforms)
        {
            if (platforms == TestPlatforms.Any)
                return true;

            if (platforms.HasFlag(TestPlatforms.Windows) && OperatingSystem.IsWindows())
                return true;
            if (platforms.HasFlag(TestPlatforms.Linux) && OperatingSystem.IsLinux())
                return true;
            if (platforms.HasFlag(TestPlatforms.OSX) && OperatingSystem.IsMacOS())
                return true;
            if (platforms.HasFlag(TestPlatforms.FreeBSD) && RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
                return true;
            if (platforms.HasFlag(TestPlatforms.iOS) && OperatingSystem.IsIOS())
                return true;
            if (platforms.HasFlag(TestPlatforms.tvOS) && OperatingSystem.IsTvOS())
                return true;
            if (platforms.HasFlag(TestPlatforms.MacCatalyst) && OperatingSystem.IsMacCatalyst())
                return true;
            if (platforms.HasFlag(TestPlatforms.Browser) && OperatingSystem.IsBrowser())
                return true;
            if (platforms.HasFlag(TestPlatforms.Android) && OperatingSystem.IsAndroid())
                return true;

            return false;
        }

        private static bool MatchesFramework(TargetFrameworkMonikers frameworks)
        {
            // NativeAOT is always .NET Core
            return frameworks.HasFlag(TargetFrameworkMonikers.Netcoreapp);
        }

        private static bool MatchesRuntime(TestRuntimes runtimes)
        {
            // NativeAOT is CoreCLR-based
            return runtimes.HasFlag(TestRuntimes.CoreCLR);
        }

        private readonly record struct ActiveIssueData(
            string Issue,
            TestPlatforms Platforms,
            TargetFrameworkMonikers Frameworks,
            TestRuntimes Runtimes,
            [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
            Type? ConditionType,
            string[] ConditionMemberNames);
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class OuterLoopAttribute : Attribute
    {
        public OuterLoopAttribute() { }
        public OuterLoopAttribute(string? reason) { }
        public OuterLoopAttribute(string? reason, TestPlatforms platforms) { }
        public OuterLoopAttribute(string? reason, TargetFrameworkMonikers frameworks) { }
        public OuterLoopAttribute(string? reason, Type conditionType, params string[] conditionMemberNames) { }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class PlatformSpecificAttribute : Attribute
    {
        public PlatformSpecificAttribute(TestPlatforms platforms) { }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class SkipOnPlatformAttribute : Attribute
    {
        public SkipOnPlatformAttribute(TestPlatforms platforms, string? reason = null) { }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class SkipOnMonoAttribute : Attribute
    {
        public SkipOnMonoAttribute(string reason) { }
        public SkipOnMonoAttribute(string reason, TestPlatforms platforms) { }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class SkipOnCoreClrAttribute : Attribute
    {
        public SkipOnCoreClrAttribute(string reason) { }
        public SkipOnCoreClrAttribute(string reason, RuntimeTestModes modes) { }
        public SkipOnCoreClrAttribute(string reason, RuntimeConfiguration configuration) { }
        public SkipOnCoreClrAttribute(string reason, RuntimeTestModes modes, RuntimeConfiguration configuration) { }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class SkipOnTargetFrameworkAttribute : Attribute
    {
        public SkipOnTargetFrameworkAttribute(TargetFrameworkMonikers frameworks, string? reason = null) { }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class SkipOnCIAttribute : Attribute
    {
        public SkipOnCIAttribute(string? reason = null) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class WindowsFullFrameworkOnlyFactAttribute : Attribute
    {
        public string? Skip { get; set; }
    }

    [Flags]
    public enum TestPlatforms
    {
        Windows = 1,
        Linux = 2,
        OSX = 4,
        FreeBSD = 8,
        NetBSD = 16,
        iOS = 32,
        tvOS = 64,
        MacCatalyst = 128,
        Browser = 256,
        Wasi = 512,
        Android = 1024,
        LinuxBionic = 2048,
        Unix = Linux | OSX | FreeBSD | NetBSD | iOS | tvOS | MacCatalyst | Android | LinuxBionic,
        AnyUnix = Unix | Browser | Wasi,
        Any = ~0,
    }

    [Flags]
    public enum TargetFrameworkMonikers
    {
        Netcoreapp = 1,
        NetFramework = 2,
        Any = ~0,
    }

    [Flags]
    public enum TestRuntimes
    {
        CoreCLR = 1,
        Mono = 2,
    }

    [Flags]
    public enum RuntimeTestModes
    {
        RegularRun = 1,
        JitStress = 2,
        JitStressRegs = 4,
        JitMinOpts = 8,
    }

    [Flags]
    public enum RuntimeConfiguration
    {
        Release = 1,
        Checked = 2,
        Debug = 4,
        Any = ~0,
    }

    public static class XunitConstants
    {
        public const string Category = "category";
        public const string IgnoreForCI = "IgnoreForCI";
        public const string OuterLoop = "OuterLoop";
        public const string Failing = "failing";
    }

    public static class CoreClrConfigurationDetection
    {
        public static bool IsStressTest => false;
    }
}

namespace Microsoft.DotNet.XUnitExtensions
{
    public class SkipTestException : Exception
    {
        public SkipTestException(string reason) : base(reason) { }
    }
}
