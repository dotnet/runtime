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

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
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

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ConditionalClassAttribute : Attribute
    {
        public ConditionalClassAttribute(Type conditionType, params string[] conditionMemberNames) { }
    }

    /// <summary>
    /// Marks a test as having a known active issue. In NativeAOT builds, this
    /// attribute extends <see cref="BeforeAfterTestAttribute"/> so that tests
    /// decorated with <c>[Fact]</c>/<c>[Theory]</c> plus <c>[ActiveIssue]</c>
    /// are skipped at runtime when the specified conditions match.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ActiveIssueAttribute : BeforeAfterTestAttribute
    {
        private readonly string _issue;
        private readonly TestPlatforms _platforms;
        private readonly TargetFrameworkMonikers _frameworks;
        private readonly TestRuntimes _runtimes;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        private readonly Type? _conditionType;
        private readonly string[] _conditionMemberNames;

        public ActiveIssueAttribute(string issue)
        {
            _issue = issue;
            _platforms = TestPlatforms.Any;
            _frameworks = TargetFrameworkMonikers.Any;
            _runtimes = TestRuntimes.CoreCLR | TestRuntimes.Mono;
            _conditionMemberNames = [];
        }

        public ActiveIssueAttribute(string issue, TestPlatforms platforms)
        {
            _issue = issue;
            _platforms = platforms;
            _frameworks = TargetFrameworkMonikers.Any;
            _runtimes = TestRuntimes.CoreCLR | TestRuntimes.Mono;
            _conditionMemberNames = [];
        }

        public ActiveIssueAttribute(string issue, TargetFrameworkMonikers frameworks)
        {
            _issue = issue;
            _platforms = TestPlatforms.Any;
            _frameworks = frameworks;
            _runtimes = TestRuntimes.CoreCLR | TestRuntimes.Mono;
            _conditionMemberNames = [];
        }

        public ActiveIssueAttribute(string issue, TestRuntimes runtimes)
        {
            _issue = issue;
            _platforms = TestPlatforms.Any;
            _frameworks = TargetFrameworkMonikers.Any;
            _runtimes = runtimes;
            _conditionMemberNames = [];
        }

        public ActiveIssueAttribute(string issue, TestPlatforms platforms, TestRuntimes runtimes)
        {
            _issue = issue;
            _platforms = platforms;
            _frameworks = TargetFrameworkMonikers.Any;
            _runtimes = runtimes;
            _conditionMemberNames = [];
        }

        public ActiveIssueAttribute(string issue, TestPlatforms platforms, TargetFrameworkMonikers frameworks, TestRuntimes runtimes)
        {
            _issue = issue;
            _platforms = platforms;
            _frameworks = frameworks;
            _runtimes = runtimes;
            _conditionMemberNames = [];
        }

        public ActiveIssueAttribute(string issue, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type conditionType, params string[] conditionMemberNames)
        {
            _issue = issue;
            _platforms = TestPlatforms.Any;
            _frameworks = TargetFrameworkMonikers.Any;
            _runtimes = TestRuntimes.CoreCLR | TestRuntimes.Mono;
            _conditionType = conditionType;
            _conditionMemberNames = conditionMemberNames;
        }

        public override void Before(ICodeGenTest test)
        {
            if (ShouldSkip())
            {
                throw SkipException.ForSkip($"Active issue: {_issue}");
            }
        }

        private bool ShouldSkip()
        {
            if (_conditionType is not null)
            {
                return EvaluateTypeConditions(_conditionType, _conditionMemberNames);
            }

            return MatchesPlatform(_platforms)
                && MatchesFramework(_frameworks)
                && MatchesRuntime(_runtimes);
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
