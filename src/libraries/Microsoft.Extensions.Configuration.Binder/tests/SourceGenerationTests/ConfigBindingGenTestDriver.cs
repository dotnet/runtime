// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using SourceGenerators.Tests;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/52062", TestPlatforms.Browser)]
    public partial class ConfigurationBindingGeneratorTests : ConfigurationBinderTestsBase
    {
        internal sealed class ConfigBindingGenTestDriver
        {
            private readonly CSharpParseOptions _parseOptions;
            private GeneratorDriver _generatorDriver;
            private SourceGenerationSpec? _genSpec;

            private readonly LanguageVersion _langVersion;
            private readonly IEnumerable<Assembly>? _assemblyReferences;
            private Compilation _compilation = null;

            public ConfigBindingGenTestDriver(
                LanguageVersion langVersion = LanguageVersion.LatestMajor,
                IEnumerable<Assembly>? assemblyReferences = null)
            {
                _langVersion = langVersion;

                _assemblyReferences = assemblyReferences ?? s_compilationAssemblyRefs;

                _parseOptions = new CSharpParseOptions(langVersion).WithFeatures(new[] {
                    new KeyValuePair<string, string>("InterceptorsPreview", "") ,
                    new KeyValuePair<string, string>("InterceptorsPreviewNamespaces", "Microsoft.Extensions.Configuration.Binder.SourceGeneration")
                });

                ConfigurationBindingGenerator generator = new() { OnSourceEmitting = spec => _genSpec = spec };
                _generatorDriver = CSharpGeneratorDriver.Create(
                    new ISourceGenerator[] { generator.AsSourceGenerator() },
                    parseOptions: _parseOptions,
                    driverOptions: new GeneratorDriverOptions(
                        disabledOutputs: IncrementalGeneratorOutputKind.None,
                        trackIncrementalGeneratorSteps: true));
            }

            public async Task<ConfigBindingGenRunResult> RunGeneratorAndUpdateCompilation(string? source = null)
            {
                await UpdateCompilationWithSource(source);
                Assert.NotNull(_compilation);

                _generatorDriver = _generatorDriver.RunGeneratorsAndUpdateCompilation(_compilation, out Compilation outputCompilation, out _, CancellationToken.None);
                GeneratorDriverRunResult runResult = _generatorDriver.GetRunResult();

                return new ConfigBindingGenRunResult
                {
                    OutputCompilation = outputCompilation,
                    Diagnostics = runResult.Diagnostics,
                    GeneratedSource = runResult.Results[0].GeneratedSources is { Length: not 0 } sources ? sources[0] : null,
                    TrackedSteps = runResult.Results[0].TrackedSteps[ConfigurationBindingGenerator.GenSpecTrackingName],
                    GenerationSpec = _genSpec
                };
            }

            private async Task UpdateCompilationWithSource(string? source = null)
            {
                if (_compilation is not null && source is not null)
                {
                    SyntaxTree newTree = CSharpSyntaxTree.ParseText(source, _parseOptions);
                    _compilation = _compilation.ReplaceSyntaxTree(_compilation.SyntaxTrees.First(), newTree);
                }
                else if (_compilation is null)
                {
                    Assert.True(source is not null, "Generator test requires input source.");
                    using AdhocWorkspace workspace = RoslynTestUtils.CreateTestWorkspace();

                    Project project = RoslynTestUtils.CreateTestProject(workspace, _assemblyReferences, langVersion: _langVersion)
                        .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Annotations))
                        .WithParseOptions(_parseOptions)
                        .WithDocuments(new string[] { source });
                    Assert.True(project.Solution.Workspace.TryApplyChanges(project.Solution));

                    _compilation = (await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false))!;
                }
            }
        }
    }

    internal struct ConfigBindingGenRunResult
    {
        public Compilation OutputCompilation { get; init; }

        public GeneratedSourceResult? GeneratedSource { get; init; }

        /// <summary>
        /// Diagnostics produced by the generator alone. Doesn't include any from other build participants.
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get; init; }

        public ImmutableArray<IncrementalGeneratorRunStep> TrackedSteps { get; init; }

        public SourceGenerationSpec? GenerationSpec { get; init; }
    }

    internal enum ExpectedDiagnostics
    {
        None,
        FromGeneratorOnly,
    }

    internal static class ConfigBindingGenTestDriverExtensions
    {
        public static void ValidateIncrementalResult(this ConfigBindingGenRunResult result,
            IncrementalStepRunReason inputReason,
            IncrementalStepRunReason outputReason)
        {
            Assert.Collection(result.TrackedSteps, step =>
            {
                Assert.Collection(step.Inputs, source => Assert.Equal(inputReason, source.Source.Outputs[source.OutputIndex].Reason));
                Assert.Collection(step.Outputs, output => Assert.Equal(outputReason, output.Reason));
            });
        }

        public static void ValidateDiagnostics(this ConfigBindingGenRunResult result, ExpectedDiagnostics expectedDiags)
        {
            ImmutableArray<Diagnostic> outputDiagnostics = result.OutputCompilation.GetDiagnostics();

            if (expectedDiags is ExpectedDiagnostics.None)
            {
                foreach (Diagnostic diagnostic in outputDiagnostics)
                {
                    Assert.True(
                        IsPermitted(diagnostic),
                        $"Generator caused diagnostic in output compilation: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}.");
                }
            }
            else
            {
                Debug.Assert(expectedDiags is ExpectedDiagnostics.FromGeneratorOnly);

                Assert.NotEmpty(result.Diagnostics);
                Assert.False(outputDiagnostics.Any(diag => !IsPermitted(diag)));
            }

            static bool IsPermitted(Diagnostic diagnostic) => diagnostic.Severity <= DiagnosticSeverity.Info;
        }
    }
}
