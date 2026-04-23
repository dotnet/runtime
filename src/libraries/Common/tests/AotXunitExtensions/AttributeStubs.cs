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

#nullable enable

using System;

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
        public bool SkipWhenEmpty { get; set; }
        public int Timeout { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ConditionalClassAttribute : Attribute
    {
        public ConditionalClassAttribute(Type conditionType, params string[] conditionMemberNames) { }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ActiveIssueAttribute : Attribute
    {
        public ActiveIssueAttribute(string issue) { }
        public ActiveIssueAttribute(string issue, TestPlatforms platforms) { }
        public ActiveIssueAttribute(string issue, TargetFrameworkMonikers frameworks) { }
        public ActiveIssueAttribute(string issue, TestRuntimes runtimes) { }
        public ActiveIssueAttribute(string issue, TestPlatforms platforms, TestRuntimes runtimes) { }
        public ActiveIssueAttribute(string issue, TestPlatforms platforms, TargetFrameworkMonikers frameworks, TestRuntimes runtimes) { }
        public ActiveIssueAttribute(string issue, Type conditionType, params string[] conditionMemberNames) { }
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
