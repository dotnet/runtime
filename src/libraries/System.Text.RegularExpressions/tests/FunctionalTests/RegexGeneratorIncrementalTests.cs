// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions.Generator;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
    public static class RegexGeneratorIncrementalTests
    {
        [Fact]
        public static async Task SameInput_DoesNotRegenerate()
        {
            Compilation compilation = await CreateCompilation(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex MyRegex();
                }");

            GeneratorDriver driver = CreateRegexGeneratorDriver();

            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];

            Assert.Collection(GetSourceGenRunSteps(runResult),
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                });

            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(GetSourceGenRunSteps(runResult),
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
                });
        }

        [Fact]
        public static async Task EquivalentSources_Regenerates()
        {
            // Unlike STJ, the Regex generator model includes Dictionary<string, string[]>
            // for helper methods, which doesn't have value equality. Equivalent sources
            // therefore produce Modified outputs rather than Unchanged.
            string source1 = @"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex MyRegex();
                }";

            string source2 = @"
                using System.Text.RegularExpressions;
                // Changing the comment and location should produce identical SG model.
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex MyRegex();
                }";

            Compilation compilation = await CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver();

            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];
            Assert.Collection(GetSourceGenRunSteps(runResult),
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                });

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8), s_parseOptions));
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(GetSourceGenRunSteps(runResult),
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));
                });
        }

        [Fact]
        public static async Task DifferentSources_Regenerates()
        {
            string source1 = @"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex MyRegex();
                }";

            string source2 = @"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""cd"")]
                    private static partial Regex MyRegex();
                }";

            Compilation compilation = await CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver();

            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];
            Assert.Collection(GetSourceGenRunSteps(runResult),
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                });

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8), s_parseOptions));
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(GetSourceGenRunSteps(runResult),
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));
                });
        }

        [Fact]
        public static async Task SourceGenModelDoesNotEncapsulateSymbolsOrCompilationData()
        {
            Compilation compilation = await CreateCompilation(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [GeneratedRegex(""ab"")]
                    private static partial Regex MyRegex();
                }");

            GeneratorDriver driver = CreateRegexGeneratorDriver();
            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];

            IncrementalGeneratorRunStep[] steps = GetSourceGenRunSteps(runResult);
            foreach (IncrementalGeneratorRunStep step in steps)
            {
                foreach ((object Value, IncrementalStepRunReason Reason) output in step.Outputs)
                {
                    WalkObjectGraph(output.Value);
                }
            }

            static void WalkObjectGraph(object obj)
            {
                var visited = new HashSet<object>();
                Visit(obj);

                void Visit(object? node)
                {
                    if (node is null || !visited.Add(node))
                    {
                        return;
                    }

                    Assert.False(node is Compilation or ISymbol, $"Model should not contain {node.GetType().Name}");

                    Type type = node.GetType();
                    if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                    {
                        return;
                    }

                    if (node is IEnumerable collection and not string)
                    {
                        foreach (object? element in collection)
                        {
                            Visit(element);
                        }

                        return;
                    }

                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        object? fieldValue = field.GetValue(node);
                        Visit(fieldValue);
                    }
                }
            }
        }

        private static readonly CSharpParseOptions s_parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        private static CSharpGeneratorDriver CreateRegexGeneratorDriver()
        {
            return CSharpGeneratorDriver.Create(
                generators: new ISourceGenerator[] { new RegexGenerator().AsSourceGenerator() },
                parseOptions: s_parseOptions,
                driverOptions: new GeneratorDriverOptions(
                    disabledOutputs: IncrementalGeneratorOutputKind.None,
                    trackIncrementalGeneratorSteps: true));
        }

        private static async Task<Compilation> CreateCompilation(string source)
        {
            var proj = new AdhocWorkspace()
                .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                .AddProject("RegexGeneratorTest", "RegexGeneratorTest.dll", "C#")
                .WithMetadataReferences(RegexGeneratorHelper.References)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithNullableContextOptions(NullableContextOptions.Enable))
                .WithParseOptions(s_parseOptions)
                .AddDocument("Test.cs", SourceText.From(source, Encoding.UTF8)).Project;

            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));

            return (await proj.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false))!;
        }

        private static IncrementalGeneratorRunStep[] GetSourceGenRunSteps(GeneratorRunResult runResult)
        {
            Assert.True(
                runResult.TrackedSteps.TryGetValue(RegexGenerator.SourceGenerationTrackingName, out var runSteps),
                $"Tracked step '{RegexGenerator.SourceGenerationTrackingName}' not found. Available: {string.Join(", ", runResult.TrackedSteps.Keys)}");

            return runSteps.ToArray();
        }
    }
}
