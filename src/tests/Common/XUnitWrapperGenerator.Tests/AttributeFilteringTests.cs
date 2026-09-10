// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace XUnitWrapperGenerator.Tests;

public class AttributeFilteringTests
{
    private const string Reason = "reason with \"quotes\"";
    private const string ReasonArgument = "\"reason with \\\"quotes\\\"\"";

    public enum Disposition
    {
        Run,
        Skip,
        Conditional
    }

    [Theory]
    [InlineData("windows", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono, Disposition.Skip)]
    [InlineData("windows", "coreclr", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono, Disposition.Run)]
    [InlineData("linux", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono, Disposition.Run)]
    [InlineData("windows", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.NetFramework, TestRuntimes.Mono, Disposition.Run)]
    [InlineData("windows", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Skip)]
    [InlineData("windows", "coreclr", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Run)]
    [InlineData("linux", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Run)]
    [InlineData("windows", null, TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Run)]
    [InlineData("WINDOWS", "MONO", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Skip)]
    [InlineData("windows", "mono", TestPlatforms.Any, TargetFrameworkMonikers.NetFramework, TestRuntimes.Any, Disposition.Run)]
    [InlineData("linux", "coreclr", TestPlatforms.Any, TargetFrameworkMonikers.Any, TestRuntimes.Any, Disposition.Skip)]
    [InlineData("windows", "coreclr", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.CoreCLR, Disposition.Skip)]
    [InlineData("windows", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.CoreCLR, Disposition.Run)]
    [InlineData("windows", "mono", TestPlatforms.Windows, 0, TestRuntimes.Any, Disposition.Run)]
    [InlineData("windows", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.Any, 0, Disposition.Run)]
    [InlineData("windows", "mono", 0, TargetFrameworkMonikers.Any, TestRuntimes.Any, Disposition.Run)]
    [InlineData("anyos", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Conditional)]
    [InlineData(null, "mono", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Conditional)]
    [InlineData("", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Conditional)]
    [InlineData("anyos", "coreclr", TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Run)]
    [InlineData("anyos", "mono", TestPlatforms.Windows, TargetFrameworkMonikers.NetFramework, TestRuntimes.Mono, Disposition.Run)]
    [InlineData("anyos", "mono", TestPlatforms.Windows | TestPlatforms.Linux, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Conditional)]
    [InlineData("anyos", "mono", TestPlatforms.Any, TargetFrameworkMonikers.Any, TestRuntimes.Mono, Disposition.Skip)]
    public void CombinedRestrictions(string? targetOS, string? runtime, TestPlatforms platforms, TargetFrameworkMonikers frameworks, TestRuntimes runtimes, Disposition expected)
    {
        string arguments = $"{ReasonArgument}, (TestPlatforms)({(int)platforms}), (TargetFrameworkMonikers)({(int)frameworks}), (TestRuntimes)({(int)runtimes})";
        Verify($"ActiveIssue({arguments})", targetOS, runtime, 0, expected, platforms, $"ActiveIssue: {Reason}");
        Verify($"OuterLoop({arguments})", targetOS, runtime, 0, expected, platforms);
        Verify($"OuterLoop({arguments})", targetOS, runtime, 1, Disposition.Run, platforms);
    }

    [Theory]
    [InlineData("", "windows", "coreclr", Disposition.Skip)]
    [InlineData(", TestPlatforms.Windows", "windows", "coreclr", Disposition.Skip)]
    [InlineData(", TestPlatforms.Windows", "linux", "mono", Disposition.Run)]
    [InlineData(", TestPlatforms.Windows", "anyos", "mono", Disposition.Conditional)]
    [InlineData(", TargetFrameworkMonikers.Netcoreapp", "linux", "coreclr", Disposition.Skip)]
    [InlineData(", TargetFrameworkMonikers.NetFramework", "windows", "mono", Disposition.Run)]
    [InlineData(", TestRuntimes.Mono", "windows", "mono", Disposition.Skip)]
    [InlineData(", TestRuntimes.Mono", "windows", "coreclr", Disposition.Run)]
    [InlineData(", TestRuntimes.CoreCLR", "linux", "coreclr", Disposition.Skip)]
    [InlineData(", typeof(TestClass), nameof(TestClass.TrueCondition)", "windows", "coreclr", Disposition.Conditional)]
    [InlineData(", typeof(TestClass), nameof(TestClass.TrueCondition), nameof(TestClass.FalseCondition)", "windows", "coreclr", Disposition.Conditional)]
    public void SimplerActiveIssueOverloads(string arguments, string targetOS, string runtime, Disposition expected)
    {
        bool? conditionRuns = arguments.Contains("typeof", StringComparison.Ordinal)
            ? arguments.Contains("FalseCondition", StringComparison.Ordinal)
            : null;
        Verify($"ActiveIssue({ReasonArgument}{arguments})", targetOS, runtime, 0, expected, TestPlatforms.Windows, $"ActiveIssue: {Reason}", conditionRuns);
    }

    [Theory]
    [InlineData("OuterLoop", 0, Disposition.Skip)]
    [InlineData("OuterLoop", 1, Disposition.Run)]
    [InlineData("OuterLoop(\"reason\")", 0, Disposition.Skip)]
    [InlineData("OuterLoop(\"reason\")", 1, Disposition.Run)]
    [InlineData("OuterLoop(\"reason\", TestRuntimes.Mono)", 0, Disposition.Skip)]
    [InlineData("OuterLoop(\"reason\", TestRuntimes.Mono)", 1, Disposition.Run)]
    public void OuterLoopPriority(string attribute, int priority, Disposition expected)
        => Verify(attribute, "windows", "mono", priority, expected, TestPlatforms.Windows);

    [Theory]
    [InlineData("windows", "mono", Disposition.Skip)]
    [InlineData("windows", "coreclr", Disposition.Run)]
    [InlineData("linux", "mono", Disposition.Run)]
    [InlineData("linux", "coreclr", Disposition.Run)]
    [InlineData("anyos", "mono", Disposition.Conditional)]
    [InlineData("anyos", "coreclr", Disposition.Run)]
    public void ActiveIssueOnReferencedEntryPoint(string targetOS, string runtime, Disposition expected)
        => Verify($"ActiveIssue({ReasonArgument}, TestPlatforms.Windows, TargetFrameworkMonikers.Any, TestRuntimes.Mono)",
            targetOS, runtime, 0, expected, TestPlatforms.Windows, $"ActiveIssue: {Reason}", fromReference: true);

    private static void Verify(string attribute, string? targetOS, string? runtime, int priority, Disposition expected, TestPlatforms platforms, string? skipReason = null, bool? conditionRuns = null, bool fromReference = false)
    {
        string source = $$"""
            using Xunit;
            public static class TestClass
            {
                public static int Calls;
                public static bool TrueCondition => true;
                public static bool FalseCondition => false;
                [Fact]
                [{{attribute}}]
                {{(fromReference ? "public static int TestBody() { Calls++; return 100; }" : "public static void TestBody() { Calls++; }")}}
            }
            """;

        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string[] referencePaths =
        [
            typeof(object).Assembly.Location,
            typeof(Console).Assembly.Location,
            typeof(FactAttribute).Assembly.Location,
            typeof(Assert).Assembly.Location,
            typeof(ActiveIssueAttribute).Assembly.Location,
            typeof(XUnitWrapperLibrary.TestSummary).Assembly.Location,
            Path.Combine(runtimeDirectory, "System.Runtime.dll"),
            Path.Combine(runtimeDirectory, "System.Collections.dll"),
            Path.Combine(runtimeDirectory, "System.Diagnostics.TraceSource.dll")
        ];
        CSharpCompilation input = CSharpCompilation.Create(
            "AttributeTest",
            [CSharpSyntaxTree.ParseText(source)],
            referencePaths.Distinct().Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        AssertNoErrors(input.GetDiagnostics());
        byte[]? testAssemblyImage = null;
        if (fromReference)
        {
            using var testAssembly = new MemoryStream();
            Assert.True(input.Emit(testAssembly).Success);
            testAssemblyImage = testAssembly.ToArray();
            input = CSharpCompilation.Create("ReferencedAttributeTest",
                references: input.References.Append(MetadataReference.CreateFromImage(testAssemblyImage)),
                options: input.Options);
        }
        input = input.WithOptions(input.Options.WithOutputKind(OutputKind.ConsoleApplication));

        // Exercise the ordinary, process-isolated, and merged runner reporters.
        for (int runner = 0; runner < 3; runner++)
        {
            var options = new Dictionary<string, string>
            {
                ["build_property.CLRTestPriorityToBuild"] = priority.ToString(),
                ["build_property.IsMergedTestRunnerAssembly"] = (runner == 2).ToString(),
                ["build_property.RequiresProcessIsolation"] = (runner == 1).ToString()
            };
            if (targetOS is not null)
                options["build_property.TargetOS"] = targetOS;
            if (runtime is not null)
                options["build_property.RuntimeFlavor"] = runtime;

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new Generator::XUnitWrapperGenerator.XUnitWrapperGenerator().AsSourceGenerator()],
                optionsProvider: new TestOptionsProvider(options));
            driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> diagnostics);
            AssertNoErrors(diagnostics);
            SyntaxNode generated = Assert.Single(driver.GetRunResult().GeneratedTrees).GetRoot();
            InvocationExpressionSyntax[] calls = generated.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(call => call.Expression.ToString() == "global::TestClass.TestBody").ToArray();
            Assert.Equal(expected == Disposition.Skip ? 0 : 1, calls.Length);
            if (calls.Length != 0)
            {
                bool hasCondition = calls[0].Ancestors().OfType<IfStatementSyntax>()
                    .Any(statement => statement.Condition.ToString().Contains("OperatingSystem.", StringComparison.Ordinal)
                        || statement.Condition.ToString().Contains("Condition", StringComparison.Ordinal));
                Assert.Equal(expected == Disposition.Conditional, hasCondition);
            }

            string code = generated.ToFullString();
            string? emittedReason = generated.DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Select(literal => literal.Token.ValueText)
                .FirstOrDefault(value => value.StartsWith("ActiveIssue:", StringComparison.Ordinal));
            Assert.Equal(runner != 0 && expected != Disposition.Run ? skipReason : null, emittedReason);
            if (runner == 2 && expected == Disposition.Skip)
                Assert.DoesNotContain("ReportPassedTest", code);
            if (runner == 2 && skipReason is not null && expected != Disposition.Run)
            {
                InvocationExpressionSyntax report = Assert.Single(generated.DescendantNodes().OfType<InvocationExpressionSyntax>(),
                    call => call.Expression.ToString() == "summary.ReportSkippedTest"
                        && call.ArgumentList.Arguments[4].Expression is LiteralExpressionSyntax);
                Assert.IsType<ReturnStatementSyntax>(((BlockSyntax)report.Parent!.Parent!).Statements.Last());
            }

            using var pe = new MemoryStream();
            var emitResult = output.Emit(pe);
            Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
            if (runner == 0)
            {
                var loadContext = new AssemblyLoadContext("AttributeTest", isCollectible: true);
                try
                {
                    Assembly? testAssembly = null;
                    if (testAssemblyImage is not null)
                    {
                        using var referencePe = new MemoryStream(testAssemblyImage);
                        testAssembly = loadContext.LoadFromStream(referencePe);
                    }
                    pe.Position = 0;
                    Assembly assembly = loadContext.LoadFromStream(pe);
                    Assert.Equal(100, assembly.EntryPoint!.Invoke(null, null));
                    bool platformMatches = (platforms.HasFlag(TestPlatforms.Windows) && OperatingSystem.IsWindows())
                        || (platforms.HasFlag(TestPlatforms.Linux) && OperatingSystem.IsLinux());
                    bool runs = expected == Disposition.Run
                        || (expected == Disposition.Conditional && (conditionRuns ?? !platformMatches));
                    Assert.Equal(runs ? 1 : 0, (testAssembly ?? assembly).GetType("TestClass")!.GetField("Calls")!.GetValue(null));
                }
                finally
                {
                    loadContext.Unload();
                }
            }
        }
    }

    private static void AssertNoErrors(IEnumerable<Diagnostic> diagnostics)
        => Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    private sealed class TestOptionsProvider(Dictionary<string, string> options) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions(options);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class TestOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
        }
    }
}
