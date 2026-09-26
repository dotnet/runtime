// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Interop.UnitTests;
using Xunit;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal class VerifyCompilationTest<T, TAnalyzer> : Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<T, TAnalyzer>.Test
        where T : new()
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public required Action<Compilation> CompilationVerifier { get; init; }

        public VerifyCompilationTest(TestTargetFramework targetFramework) : base(targetFramework)
        {
        }

        public VerifyCompilationTest(bool referenceAncillaryInterop) : base(referenceAncillaryInterop)
        {
        }

        protected override void VerifyFinalCompilation(Compilation compilation) => CompilationVerifier(compilation);
    }

    internal static class GeneratedSourceVerification
    {
        public static void VerifyIncrementalOutput(
            IIncrementalGenerator generator,
            string source,
            string updatedSource,
            bool outputChanges,
            int expectedSourceCount,
            params string[] generationSteps)
        {
            Compilation compilation = TestUtils.CreateCompilation(source);
            GeneratorDriver driver = TestUtils.CreateDriver(
                compilation,
                options: null,
                [generator],
                new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation firstCompilation, out var diagnostics);
            Assert.Empty(diagnostics);
            TestUtils.AssertPostSourceGeneratorCompilation(firstCompilation);
            GeneratorRunResult firstResult = Assert.Single(driver.GetRunResult().Results);
            Assert.Equal(expectedSourceCount, firstResult.GeneratedSources.Length);

            SyntaxTree tree = Assert.Single(compilation.SyntaxTrees);
            compilation = compilation.ReplaceSyntaxTree(tree, tree.WithChangedText(SourceText.From(updatedSource)));
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation secondCompilation, out diagnostics);
            Assert.Empty(diagnostics);
            TestUtils.AssertPostSourceGeneratorCompilation(secondCompilation);
            GeneratorRunResult secondResult = Assert.Single(driver.GetRunResult().Results);
            Assert.Equal(expectedSourceCount, secondResult.GeneratedSources.Length);

            foreach (string step in generationSteps)
            {
                var outputs = secondResult.TrackedSteps[step].SelectMany(static run => run.Outputs).ToArray();
                Assert.NotEmpty(outputs);
                Assert.All(outputs, static output => Assert.True(output.Value is string or ValueTuple<string, string>));
                if (!outputChanges)
                {
                    Assert.All(outputs, static output => Assert.True(output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));
                }
            }

            var outputReasons = secondResult.TrackedOutputSteps.Values
                .SelectMany(static steps => steps)
                .SelectMany(static step => step.Outputs)
                .Select(static output => output.Reason)
                .ToArray();
            Assert.NotEmpty(outputReasons);

            var firstSources = firstResult.GeneratedSources.Select(static generated => (generated.HintName, generated.SourceText.ToString())).ToArray();
            var secondSources = secondResult.GeneratedSources.Select(static generated => (generated.HintName, generated.SourceText.ToString())).ToArray();
            if (outputChanges)
            {
                Assert.Contains(IncrementalStepRunReason.Modified, outputReasons);
                Assert.False(firstSources.SequenceEqual(secondSources));
            }
            else
            {
                Assert.All(outputReasons, static reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
                Assert.Equal(firstSources, secondSources);
            }

            foreach (GeneratedSourceResult generated in secondResult.GeneratedSources)
            {
                string text = generated.SourceText.ToString();
                Assert.EndsWith("\r\n", text);
                Assert.DoesNotContain("\n", text.Replace("\r\n", ""));
                Assert.All(text.Split(["\r\n"], StringSplitOptions.None), static line => Assert.Equal(line.TrimEnd(), line));
            }
        }
    }
}
