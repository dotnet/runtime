// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer.Tests
{
	internal static class TestCaseCompilation
	{
		private static readonly ImmutableArray<DiagnosticAnalyzer> SupportedDiagnosticAnalyzers =
			ImmutableArray.Create<DiagnosticAnalyzer> (
				new RequiresDynamicCodeAnalyzer (),
				new COMAnalyzer (),
				new RequiresAssemblyFilesAnalyzer (),
				new RequiresUnreferencedCodeAnalyzer ());

		public static Task<(CompilationWithAnalyzers Compilation, SemanticModel SemanticModel)> CreateCompilation (
			string src,
			(string, string)[]? globalAnalyzerOptions = null,
			IEnumerable<MetadataReference>? additionalReferences = null,
			IEnumerable<SyntaxTree>? additionalSources = null)
			=> CreateCompilation (CSharpSyntaxTree.ParseText (src), globalAnalyzerOptions, additionalReferences, additionalSources);

		public static async Task<(CompilationWithAnalyzers Compilation, SemanticModel SemanticModel)> CreateCompilation (
			SyntaxTree src,
			(string, string)[]? globalAnalyzerOptions = null,
			IEnumerable<MetadataReference>? additionalReferences = null,
			IEnumerable<SyntaxTree>? additionalSources = null)
		{
			var mdRef = MetadataReference.CreateFromFile (typeof (Mono.Linker.Tests.Cases.Expectations.Metadata.BaseMetadataAttribute).Assembly.Location);
			additionalReferences ??= Array.Empty<MetadataReference> ();
			var sources = new List<SyntaxTree> () { src };
			sources.AddRange (additionalSources ?? Array.Empty<SyntaxTree> ());
			var comp = CSharpCompilation.Create (
				assemblyName: Guid.NewGuid ().ToString ("N"),
				syntaxTrees: sources,
				references: (await TestCaseUtils.GetNet6References ()).Add (mdRef).AddRange (additionalReferences),
				new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary));

			var analyzerOptions = new AnalyzerOptions (
				ImmutableArray<AdditionalText>.Empty,
				new SimpleAnalyzerOptions (globalAnalyzerOptions));

			var compWithAnalyzerOptions = new CompilationWithAnalyzersOptions (
				analyzerOptions,
				(_1, _2, _3) => { },
				concurrentAnalysis: true,
				logAnalyzerExecutionTime: false);

			return (new CompilationWithAnalyzers (comp, SupportedDiagnosticAnalyzers, compWithAnalyzerOptions), comp.GetSemanticModel (src));
		}

		public static async Task<Compilation> GetCompilation (string source, IEnumerable<MetadataReference>? additionalReferences = null)
			=> (await CreateCompilation (source, additionalReferences: additionalReferences ?? Array.Empty<MetadataReference> ())).Compilation.Compilation;

		class SimpleAnalyzerOptions : AnalyzerConfigOptionsProvider
		{
			public SimpleAnalyzerOptions ((string, string)[]? globalOptions)
			{
				globalOptions ??= Array.Empty<(string, string)> ();
				GlobalOptions = new SimpleAnalyzerConfigOptions (ImmutableDictionary.CreateRange (
					StringComparer.OrdinalIgnoreCase,
					globalOptions.Select (x => new KeyValuePair<string, string> (x.Item1, x.Item2))));
			}

			public override AnalyzerConfigOptions GlobalOptions { get; }

			public override AnalyzerConfigOptions GetOptions (SyntaxTree tree)
				=> SimpleAnalyzerConfigOptions.Empty;

			public override AnalyzerConfigOptions GetOptions (AdditionalText textFile)
				=> SimpleAnalyzerConfigOptions.Empty;

			class SimpleAnalyzerConfigOptions : AnalyzerConfigOptions
			{
				public static readonly SimpleAnalyzerConfigOptions Empty = new SimpleAnalyzerConfigOptions (ImmutableDictionary<string, string>.Empty);

				private readonly ImmutableDictionary<string, string> _dict;
				public SimpleAnalyzerConfigOptions (ImmutableDictionary<string, string> dict)
				{
					_dict = dict;
				}

				// Suppress warning about missing nullable attributes
#pragma warning disable 8765
				public override bool TryGetValue (string key, out string? value)
					=> _dict.TryGetValue (key, out value);
#pragma warning restore 8765
			}
		}
	}
}
