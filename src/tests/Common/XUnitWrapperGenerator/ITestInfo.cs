// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XUnitWrapperGenerator;

interface ITestInfo
{
    string ExecutionStatement { get; }
}

sealed class BasicTestMethod : ITestInfo
{
    public BasicTestMethod(IMethodSymbol method, string externAlias, ImmutableArray<string> arguments = default)
    {
        var args = arguments.IsDefaultOrEmpty ? "" : string.Join(", ", arguments);
        string containingType = method.ContainingType.ToDisplayString(XUnitWrapperGenerator.FullyQualifiedWithoutGlobalNamespace);
        if (method.IsStatic)
        {
            ExecutionStatement = $"{externAlias}::{containingType}.{method.Name}({args});";
        }
        else
        {
            ExecutionStatement = $"using ({externAlias}::{containingType} obj = new()) obj.{method.Name}({args});";
        }
    }

    public string ExecutionStatement { get; }

    public override bool Equals(object obj)
    {
        return obj is BasicTestMethod other && ExecutionStatement == other.ExecutionStatement;
    }
}

sealed class ConditionalTest : ITestInfo
{
    public ConditionalTest(ITestInfo innerTest, string condition)
    {
        ExecutionStatement = $"if ({condition}) {{ {innerTest.ExecutionStatement} }}";
    }

    public string ExecutionStatement { get; }

    public override bool Equals(object obj)
    {
        return obj is ConditionalTest other && ExecutionStatement == other.ExecutionStatement;
    }
}

sealed class PlatformSpecificTest : ITestInfo
{
    public PlatformSpecificTest(ITestInfo innerTest, Xunit.TestPlatforms platform)
    {
        List<string> platformCheckConditions = new();
        if (platform.HasFlag(Xunit.TestPlatforms.Windows))
        {
            platformCheckConditions.Add("global::System.OperatingSystem.IsWindows()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.Linux))
        {
            platformCheckConditions.Add("global::System.OperatingSystem.IsLinux()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.OSX))
        {
            platformCheckConditions.Add("global::System.OperatingSystem.IsMacOS()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.illumos))
        {
            platformCheckConditions.Add(@"global::System.OperatingSystem.IsOSPlatform(""illumos"")");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.Solaris))
        {
            platformCheckConditions.Add(@"global::System.OperatingSystem.IsOSPlatform(""Solaris"")");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.Android))
        {
            platformCheckConditions.Add("global::System.OperatingSystem.IsAndroid()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.iOS))
        {
            platformCheckConditions.Add("global::System.OperatingSystem.IsIOS()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.tvOS))
        {
            platformCheckConditions.Add("global::System.OperatingSystem.IsAndroid()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.MacCatalyst))
        {
            platformCheckConditions.Add(@"global::System.OperatingSystem.IsOSPlatform(""maccatalyst"")");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.Browser))
        {
            platformCheckConditions.Add(@"global::System.OperatingSystem.IsOSPlatform(""browser"")");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.FreeBSD))
        {
            platformCheckConditions.Add(@"global::System.OperatingSystem.IsFreeBSD()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.NetBSD))
        {
            platformCheckConditions.Add(@"global::System.OperatingSystem.IsOSPlatform(""NetBSD"")");
        }
        ExecutionStatement = $"if ({string.Join(" || ", platformCheckConditions)}) {{ {innerTest.ExecutionStatement} }}";
    }

    public string ExecutionStatement { get; }

    public override bool Equals(object obj)
    {
        return obj is PlatformSpecificTest other && ExecutionStatement == other.ExecutionStatement;
    }
}

sealed class MemberDataTest : ITestInfo
{
    public MemberDataTest(ISymbol referencedMember, ITestInfo innerTest, string externAlias, string argumentLoopVarIdentifier)
    {
        string containingType = referencedMember.ContainingType.ToDisplayString(XUnitWrapperGenerator.FullyQualifiedWithoutGlobalNamespace);
        string memberInvocation = referencedMember switch
        {
            IPropertySymbol { IsStatic: true } => $"{externAlias}::{containingType}.{referencedMember.Name}",
            IMethodSymbol { IsStatic: true, Parameters: { Length: 0 } } => $"{externAlias}::{containingType}.{referencedMember.Name}()",
            _ => throw new ArgumentException()
        };
        ExecutionStatement = $@"
foreach (object[] {argumentLoopVarIdentifier} in {memberInvocation})
{{
    {innerTest.ExecutionStatement}
}}
";
    }

    public string ExecutionStatement { get; }
}
