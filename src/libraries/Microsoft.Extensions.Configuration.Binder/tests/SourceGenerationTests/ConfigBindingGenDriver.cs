// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
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
        internal sealed class ConfigBindingGenDriver : IDisposable
        {
            private readonly CSharpParseOptions _parseOptions;
            private readonly CSharpGeneratorDriver _csharpGenerationDriver;

            private readonly AdhocWorkspace _workspace;
            private Project _project;

            public ConfigBindingGenDriver(
                LanguageVersion langVersion = LanguageVersion.LatestMajor,
                IEnumerable<Assembly>? assemblyReferences = null)
            {
                assemblyReferences ??= s_compilationAssemblyRefs;

                _parseOptions = new CSharpParseOptions(langVersion).WithFeatures(new[] { 
                    new KeyValuePair<string, string>("InterceptorsPreview", "") ,
                    new KeyValuePair<string, string>("InterceptorsPreviewNamespaces", "Microsoft.Extensions.Configuration.Binder.SourceGeneration")
                });

                _workspace = RoslynTestUtils.CreateTestWorkspace();

                _project = RoslynTestUtils.CreateTestProject(_workspace, assemblyReferences, langVersion: langVersion)
                    .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Annotations))
                    .WithParseOptions(_parseOptions);


                _csharpGenerationDriver = CSharpGeneratorDriver.Create(new[] { new ConfigurationBindingGenerator().AsSourceGenerator() }, parseOptions: _parseOptions);
            }

            public async Task<ConfigBindingGenResult> RunGeneratorAndUpdateCompilation(params string[]? sources)
            {
                if (sources is not null)
                {
                    _project = _project.WithDocuments(sources);
                    Assert.True(_project.Solution.Workspace.TryApplyChanges(_project.Solution));
                }

                Compilation compilation = (await _project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false))!;
                GeneratorDriver generatorDriver = _csharpGenerationDriver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out _, CancellationToken.None);
                GeneratorDriverRunResult runDriverRunResult = generatorDriver.GetRunResult();

                return new ConfigBindingGenResult
                {
                    OutputCompilation = outputCompilation,
                    Diagnostics = runDriverRunResult.Diagnostics,
                    GeneratedSources = runDriverRunResult.Results[0].GeneratedSources,
                };
            }

            public void Dispose() => _workspace.Dispose();
        }

        internal struct ConfigBindingGenResult
        {
            public required Compilation OutputCompilation { get; init; }

            public required ImmutableArray<GeneratedSourceResult> GeneratedSources { get; init; }

            /// <summary>
            /// Diagnostics produced by the generator alone. Doesn't include any from other build participants.
            /// </summary>
            public required ImmutableArray<Diagnostic> Diagnostics { get; init; }
        }
    }

    internal static class ConfigBindinGenDriverExtensions
    {
        public static void AssertHasSourceAndNoDiagnostics(this ConfigurationBindingGeneratorTests.ConfigBindingGenResult result)
        {
            Assert.Single(result.GeneratedSources);
            Assert.NotEmpty(result.Diagnostics);
        }

        public static void AssertHasSourceAndDiagnostics(this ConfigurationBindingGeneratorTests.ConfigBindingGenResult result)
        {
            Assert.Single(result.GeneratedSources);
            Assert.NotEmpty(result.Diagnostics);
        }

        public static void AssertEmpty(this ConfigurationBindingGeneratorTests.ConfigBindingGenResult result)
        {
            Assert.Empty(result.GeneratedSources);
            Assert.Empty(result.Diagnostics);
        }
    }
}
