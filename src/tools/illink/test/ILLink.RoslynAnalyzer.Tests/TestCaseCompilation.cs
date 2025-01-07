// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
				new RequiresUnreferencedCodeAnalyzer (),
				new DynamicallyAccessedMembersAnalyzer ());

		public static (CompilationWithAnalyzers Compilation, SemanticModel SemanticModel, List<Diagnostic> ExceptionDiagnostics) CreateCompilation (
			string src,
			bool consoleApplication,
			(string, string)[]? globalAnalyzerOptions = null,
			IEnumerable<MetadataReference>? additionalReferences = null,
			IEnumerable<SyntaxTree>? additionalSources = null,
			IEnumerable<AdditionalText>? additionalFiles = null)
			=> CreateCompilation (CSharpSyntaxTree.ParseText (src), consoleApplication, globalAnalyzerOptions, additionalReferences, additionalSources, additionalFiles);

		public static (CompilationWithAnalyzers Compilation, SemanticModel SemanticModel, List<Diagnostic> ExceptionDiagnostics) CreateCompilation (
			SyntaxTree src,
			bool consoleApplication,
			(string, string)[]? globalAnalyzerOptions = null,
			IEnumerable<MetadataReference>? additionalReferences = null,
			IEnumerable<SyntaxTree>? additionalSources = null,
			IEnumerable<AdditionalText>? additionalFiles = null)
		{
			var mdRef = MetadataReference.CreateFromFile (typeof (Mono.Linker.Tests.Cases.Expectations.Metadata.BaseMetadataAttribute).Assembly.Location);
			additionalReferences ??= Array.Empty<MetadataReference> ();
			var sources = new List<SyntaxTree> () { src };
			sources.AddRange (additionalSources ?? Array.Empty<SyntaxTree> ());
			var comp = CSharpCompilation.Create (
				assemblyName: Guid.NewGuid ().ToString ("N"),
				syntaxTrees: sources,
				references: SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences().Add(mdRef).AddRange(additionalReferences),
				new CSharpCompilationOptions (consoleApplication ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary));
			var analyzerOptions = new AnalyzerOptions (
				additionalFiles: additionalFiles?.ToImmutableArray () ?? ImmutableArray<AdditionalText>.Empty,
				new SimpleAnalyzerOptions (globalAnalyzerOptions));

			var exceptionDiagnostics = new List<Diagnostic> ();

			var compWithAnalyzerOptions = new CompilationWithAnalyzersOptions (
				analyzerOptions,
				(Exception exception, DiagnosticAnalyzer diagnosticAnalyzer, Diagnostic diagnostic) => {
					exceptionDiagnostics.Add (diagnostic);
				},
				concurrentAnalysis: true,
				logAnalyzerExecutionTime: false);

			return (new CompilationWithAnalyzers (comp, SupportedDiagnosticAnalyzers, compWithAnalyzerOptions), comp.GetSemanticModel (src), exceptionDiagnostics);
		}

		sealed class SimpleAnalyzerOptions : AnalyzerConfigOptionsProvider
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

			sealed class SimpleAnalyzerConfigOptions : AnalyzerConfigOptions
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
