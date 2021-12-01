// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace XUnitWrapperGenerator;

interface ITestInfo
{
    string TestNameExpression { get; }
    string DisplayNameForFiltering { get; }
    string Method { get; }
    string ContainingType { get; }

    string GenerateTestExecution(ITestReporterWrapper testReporterWrapper);
}

interface ITestReporterWrapper
{
    string WrapTestExecutionWithReporting(string testExecution, ITestInfo test);

    string GenerateSkippedTestReporting(ITestInfo skippedTest);
}

sealed class BasicTestMethod : ITestInfo
{
    public BasicTestMethod(IMethodSymbol method, string externAlias, ImmutableArray<string> arguments = default, string? displayNameExpression = null)
    {
        var args = arguments.IsDefaultOrEmpty ? "" : string.Join(", ", arguments);
        ContainingType = method.ContainingType.ToDisplayString(XUnitWrapperGenerator.FullyQualifiedWithoutGlobalNamespace);
        Method = method.Name;
        DisplayNameForFiltering = $"{ContainingType}.{Method}({args})";
        TestNameExpression = displayNameExpression ?? $"\"{externAlias}::{ContainingType}.{Method}({args})\"";
        if (method.IsStatic)
        {
            ExecutionStatement = $"{externAlias}::{ContainingType}.{Method}({args});";
        }
        else
        {
            ExecutionStatement = $"using ({externAlias}::{ContainingType} obj = new()) obj.{Method}({args});";
        }
    }

    public string TestNameExpression { get; }
    public string DisplayNameForFiltering { get; }
    public string Method { get; }
    public string ContainingType { get; }
    private string ExecutionStatement { get; }

    public string GenerateTestExecution(ITestReporterWrapper testReporterWrapper)
    {
        return testReporterWrapper.WrapTestExecutionWithReporting(ExecutionStatement, this);
    }

    public override bool Equals(object obj)
    {
        return obj is BasicTestMethod other
            && TestNameExpression == other.TestNameExpression
            && Method == other.Method
            && ContainingType == other.ContainingType
            && ExecutionStatement == other.ExecutionStatement;
    }
}
sealed class LegacyStandaloneEntryPointTestMethod : ITestInfo
{
    public LegacyStandaloneEntryPointTestMethod(IMethodSymbol method, string externAlias)
    {
        ContainingType = method.ContainingType.ToDisplayString(XUnitWrapperGenerator.FullyQualifiedWithoutGlobalNamespace);
        Method = method.Name;
        TestNameExpression = $"\"{externAlias}::{ContainingType}.{Method}()\"";
        DisplayNameForFiltering = $"{ContainingType}.{Method}()";
        ExecutionStatement = $"Xunit.Assert.Equal(100, {externAlias}::{ContainingType}.{Method}());";
    }

    public string TestNameExpression { get; }
    public string DisplayNameForFiltering { get; }

    public string Method { get; }
    public string ContainingType { get; }
    private string ExecutionStatement { get; }

    public string GenerateTestExecution(ITestReporterWrapper testReporterWrapper)
    {
        return testReporterWrapper.WrapTestExecutionWithReporting(ExecutionStatement, this);
    }

    public override bool Equals(object obj)
    {
        return obj is LegacyStandaloneEntryPointTestMethod other
            && TestNameExpression == other.TestNameExpression
            && Method == other.Method
            && ContainingType == other.ContainingType; ;
    }
}

sealed class ConditionalTest : ITestInfo
{
    private ITestInfo _innerTest;
    private string _condition;
    public ConditionalTest(ITestInfo innerTest, string condition)
    {
        _innerTest = innerTest;
        _condition = condition;
        TestNameExpression = innerTest.TestNameExpression;
        DisplayNameForFiltering = innerTest.DisplayNameForFiltering;
        Method = innerTest.Method;
        ContainingType = innerTest.ContainingType;
    }

    public ConditionalTest(ITestInfo innerTest, Xunit.TestPlatforms platform)
        : this(innerTest, GetPlatformConditionFromTestPlatform(platform))
    {
    }

    public string TestNameExpression { get; }

    public string DisplayNameForFiltering { get; }

    public string Method { get; }
    public string ContainingType { get; }

    public string GenerateTestExecution(ITestReporterWrapper testReporterWrapper)
    {
        return $"if ({_condition}) {{ {_innerTest.GenerateTestExecution(testReporterWrapper)} }} else {{ {testReporterWrapper.GenerateSkippedTestReporting(_innerTest)} }}";
    }

    public override bool Equals(object obj)
    {
        return obj is ConditionalTest other
            && TestNameExpression == other.TestNameExpression
            && Method == other.Method
            && ContainingType == other.ContainingType
            && _condition == other._condition
            && _innerTest.Equals(other._innerTest);
    }

    private static string GetPlatformConditionFromTestPlatform(Xunit.TestPlatforms platform)
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
            platformCheckConditions.Add("(global::System.OperatingSystem.IsIOS() && !global::System.OperatingSystem.IsMacCatalyst())");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.tvOS))
        {
            platformCheckConditions.Add("global::System.OperatingSystem.IsTvOS()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.MacCatalyst))
        {
            platformCheckConditions.Add("global::System.OperatingSystem.IsMacCatalyst()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.Browser))
        {
            platformCheckConditions.Add("global::System.OperatingSystem.IsBrowser()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.FreeBSD))
        {
            platformCheckConditions.Add(@"global::System.OperatingSystem.IsFreeBSD()");
        }
        if (platform.HasFlag(Xunit.TestPlatforms.NetBSD))
        {
            platformCheckConditions.Add(@"global::System.OperatingSystem.IsOSPlatform(""NetBSD"")");
        }
        return string.Join(" || ", platformCheckConditions);
    }
}

sealed class MemberDataTest : ITestInfo
{
    private ITestInfo _innerTest;
    private string _memberInvocation;
    private string _loopVarIdentifier;
    public MemberDataTest(ISymbol referencedMember, ITestInfo innerTest, string externAlias, string argumentLoopVarIdentifier)
    {
        TestNameExpression = innerTest.TestNameExpression;
        Method = innerTest.Method;
        ContainingType = innerTest.ContainingType;
        DisplayNameForFiltering = $"{ContainingType}.{Method}(...)";
        _innerTest = innerTest;
        _loopVarIdentifier = argumentLoopVarIdentifier;

        string containingType = referencedMember.ContainingType.ToDisplayString(XUnitWrapperGenerator.FullyQualifiedWithoutGlobalNamespace);
        _memberInvocation = referencedMember switch
        {
            IPropertySymbol { IsStatic: true } => $"{externAlias}::{containingType}.{referencedMember.Name}",
            IMethodSymbol { IsStatic: true, Parameters: { Length: 0 } } => $"{externAlias}::{containingType}.{referencedMember.Name}()",
            _ => throw new ArgumentException()
        };
    }

    public string TestNameExpression { get; }
    public string DisplayNameForFiltering { get; }
    public string Method { get; }
    public string ContainingType { get; }

    public string GenerateTestExecution(ITestReporterWrapper testReporterWrapper)
    {
        return $@"
foreach (object[] {_loopVarIdentifier} in {_memberInvocation})
{{
    {_innerTest.GenerateTestExecution(testReporterWrapper)}
}}
";
    }

    public override bool Equals(object obj)
    {
        return obj is MemberDataTest other
            && TestNameExpression == other.TestNameExpression
            && Method == other.Method
            && ContainingType == other.ContainingType
            && _innerTest.Equals(other._innerTest);
    }
}

sealed class OutOfProcessTest : ITestInfo
{
    public OutOfProcessTest(string displayName, string relativeAssemblyPath)
    {
        Method = displayName;
        DisplayNameForFiltering = displayName;
        TestNameExpression = $"@\"{displayName}\"";
        ExecutionStatement = $@"TestLibrary.OutOfProcessTest.RunOutOfProcessTest(typeof(Program).Assembly.Location, @""{relativeAssemblyPath}"");";
    }

    public string TestNameExpression { get; }

    public string DisplayNameForFiltering { get; }

    public string Method { get; }

    public string ContainingType => "OutOfProcessTest";

    private string ExecutionStatement { get; }

    public string GenerateTestExecution(ITestReporterWrapper testReporterWrapper) => testReporterWrapper.WrapTestExecutionWithReporting(ExecutionStatement, this);
}

sealed class NoTestReporting : ITestReporterWrapper
{
    public string WrapTestExecutionWithReporting(string testExecution, ITestInfo test) => testExecution;

    public string GenerateSkippedTestReporting(ITestInfo skippedTest) => string.Empty;
}

sealed class WrapperLibraryTestSummaryReporting : ITestReporterWrapper
{
    private string _summaryLocalIdentifier;
    private readonly string _filterLocalIdentifier;

    public WrapperLibraryTestSummaryReporting(string summaryLocalIdentifier, string filterLocalIdentifier)
    {
        _summaryLocalIdentifier = summaryLocalIdentifier;
        _filterLocalIdentifier = filterLocalIdentifier;
    }

    public string WrapTestExecutionWithReporting(string testExecutionExpression, ITestInfo test)
    {
        StringBuilder builder = new();
        builder.AppendLine($"if ({_filterLocalIdentifier} is null || {_filterLocalIdentifier}.ShouldRunTest(@\"{test.ContainingType}.{test.Method}\", {test.TestNameExpression}))");
        builder.AppendLine("{");

        builder.AppendLine($"TimeSpan testStart = stopwatch.Elapsed;");
        builder.AppendLine("try {");
        builder.AppendLine(testExecutionExpression);
        builder.AppendLine($"{_summaryLocalIdentifier}.ReportPassedTest({test.TestNameExpression}, \"{test.ContainingType}\", @\"{test.Method}\", stopwatch.Elapsed - testStart);");
        builder.AppendLine("}");
        builder.AppendLine("catch (System.Exception ex) {");
        builder.AppendLine($"{_summaryLocalIdentifier}.ReportFailedTest({test.TestNameExpression}, \"{test.ContainingType}\", @\"{test.Method}\", stopwatch.Elapsed - testStart, ex);");
        builder.AppendLine("}");

        builder.AppendLine("}");
        return builder.ToString();
    }

    public string GenerateSkippedTestReporting(ITestInfo skippedTest)
    {
        return $"{_summaryLocalIdentifier}.ReportSkippedTest({skippedTest.TestNameExpression}, \"{skippedTest.ContainingType}\", \"{skippedTest.Method}\", TimeSpan.Zero, string.Empty);";
    }
}
