// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.RoslynAnalyzer.Tests
{
	sealed class TestCases
	{
		// Maps from suite name to a set of testcase names.
		// Suite name is:
		// - The namespace of the test class, minus "Mono.Linker.Tests.Cases", for ILLink tests
		// - The namespace + test class name, minus "ILLink.RoslynAnalyzer.Tests", for analyzer tests
		// Testcase name is:
		// - The test class name, for ILLink tests
		// - The test fact method name, minus "Tests", for analyzer tests
		// For example:
		// | Testcase                | Suite name           | Linker                                     | Analyzer                                |
		// |-------------------------+----------------------+--------------------------------------------+-----------------------------------------|
		// | RequiresCapability      | RequiresCapability   | RequiresCapability class in namespcae      | RequiresCapability method in class      |
		// |                         |                      | Mono.Linker.Tests.Cases.RequiresCapability | RequiresCapabilityTests in namespace    |
		// |                         |                      |                                            | ILLink.RoslynAnalyzer.Tests             |
		// |-------------------------+----------------------+--------------------------------------------+-----------------------------------------|
		// | RequiresOnAttributeCtor | RequiresCapability   | RequiresOnAttributeCtor class in namespace | RequiresOnAttributeCtor method in class |
		// |                         |                      | Mono.Linker.Tests.Cases.RequiresCapability | RequiresCapabilityTests in namespace    |
		// |                         |                      |                                            | ILlink.RoslynAnalyzer.Tests             |
		// |-------------------------+----------------------+--------------------------------------------+-----------------------------------------|
		// | UnusedPInvoke           | Interop.PInvokeTests | UnusedPInvoke class in namespace           | UnusedPInvoke method in class           |
		// |                         |                      | Mono.Linker.Tests.Cases.Interop.PInvoke    | PinvokeTests in namespace               |
		// |                         |                      |                                            | ILLink.RoslynAnalyzer.Tests.Interop     |
		public readonly Dictionary<string, HashSet<string>> Suites = new ();

		public void Add (string suiteName, string name)
		{
			if (!Suites.TryGetValue (suiteName, out var testCases)) {
				testCases = new HashSet<string> ();
				Suites.Add (suiteName, testCases);
			}

			testCases.Add (name);
		}
	}

	public static class TestClassGenerator
	{
		public const string TestNamespace = "ILLink.RoslynAnalyzer.Tests";

		public static string GenerateClassHeader (string suiteName, bool newTestSuite)
		{
			int idx = suiteName.LastIndexOf ('.');
			// Test suite class from innermost namespace part
			var suiteClassName = suiteName.Substring (idx + 1);
			// Namespace from outer namespaces, or empty
			var suiteNamespacePart = suiteClassName.Length < suiteName.Length ?
				$".{suiteName.Substring (0, idx)}" : string.Empty;

			string header = $@"using System;
using System.Threading.Tasks;
using Xunit;

namespace {TestNamespace}{suiteNamespacePart}
{{
	public sealed partial class {suiteClassName}Tests : LinkerTestBase
	{{
";
			if (newTestSuite)
				header += $@"
		protected override string TestSuiteName => ""{suiteName}"";
";
			return header;
		}

		public static string GenerateFact (string testCase)
		{
			return $@"
		[Fact]
		public Task {testCase} ()
		{{
			return RunTest (allowMissingWarnings: true);
		}}
";
		}

		public static string GenerateClassFooter ()
		{
			return $@"
	}}
}}";
		}
	}

	[Generator]
	public class TestCaseGenerator : IIncrementalGenerator
	{
		public const string TestCaseAssembly = "Mono.Linker.Tests.Cases";

		public void Execute (GeneratorExecutionContext context)
		{
			IAssemblySymbol? assemblySymbol = null;

			// Find testcase assembly
			foreach (var reference in context.Compilation.References) {
				ISymbol? assemblyOrModule = context.Compilation.GetAssemblyOrModuleSymbol (reference);
				if (assemblyOrModule is IAssemblySymbol asmSym && asmSym.Name == TestCaseAssembly) {
					assemblySymbol = asmSym;
					break;
				}
			}
			if (assemblySymbol is null || assemblySymbol.GetMetadata () is not AssemblyMetadata assemblyMetadata)
				return;

			ModuleMetadata moduleMetadata = assemblyMetadata.GetModules ().Single ();
			MetadataReader metadataReader = moduleMetadata.GetMetadataReader ();
			TestCases testCases = new ();
			string suiteName;

			// Find test classes to generate
			foreach (var typeDefHandle in metadataReader.TypeDefinitions) {
				TypeDefinition typeDef = metadataReader.GetTypeDefinition (typeDefHandle);
				// Must not be a nested type
				if (typeDef.IsNested)
					continue;

				string ns = metadataReader.GetString (typeDef.Namespace);
				// Must be in the testcases namespace
				if (!ns.StartsWith (TestCaseAssembly))
					continue;

				// Must have a Main method
				bool hasMain = false;
				foreach (var methodDefHandle in typeDef.GetMethods ()) {
					MethodDefinition methodDef = metadataReader.GetMethodDefinition (methodDefHandle);
					if (metadataReader.GetString (methodDef.Name) == "Main") {
						hasMain = true;
						break;
					}
				}
				if (!hasMain)
					continue;

				string testName = metadataReader.GetString (typeDef.Name);
				suiteName = ns.Substring (TestCaseAssembly.Length + 1);

				testCases.Add (suiteName, testName);
			}
		}

		static string GetFullName(ClassDeclarationSyntax classSyntax)
		{
			return GetFullName(classSyntax)!;

			static string? GetFullName(SyntaxNode? node) {
				if (node == null)
					return null;

				var name = node switch {
					ClassDeclarationSyntax classSyntax => $"{classSyntax.Identifier.ValueText}",
					NamespaceDeclarationSyntax namespaceSyntax => $"{namespaceSyntax.Name}",
					CompilationUnitSyntax => null,
					_ => throw new NotImplementedException($"GetFullName for node type {node.GetType()}")
				};

				if (name == null)
					return null;

				return GetFullName(node.Parent) is string parentName
					? $"{parentName}.{name}"
					: name;
			}
		}

		public void Initialize (IncrementalGeneratorInitializationContext context)
		{
			IncrementalValuesProvider<MetadataReference> metadataReferences = context.MetadataReferencesProvider;

			IncrementalValueProvider<Compilation> compilation = context.CompilationProvider;

			// For any steps below which can fail (for example, getting metadata might return null),
			// we use SelectMany to return zero or one results.
			IncrementalValuesProvider<IAssemblySymbol> assemblySymbol = metadataReferences
				.Combine(compilation)
				.SelectMany(static (combined, _) => {
					var (reference, compilation) = combined;
					return compilation.GetAssemblyOrModuleSymbol (reference) is IAssemblySymbol asmSym && asmSym.Name == TestCaseAssembly
						? ImmutableArray.Create(asmSym)
						: ImmutableArray<IAssemblySymbol>.Empty;
				});

			IncrementalValuesProvider<AssemblyMetadata> assemblyMetadata = assemblySymbol
				.SelectMany(static (symbol, _) =>
					symbol.GetMetadata() is AssemblyMetadata metadata
						? ImmutableArray.Create(metadata)
						: ImmutableArray<AssemblyMetadata>.Empty);

			IncrementalValuesProvider<ModuleMetadata> moduleMetadata = assemblyMetadata
				.Select(static (metadata, _) =>
					metadata.GetModules().Single());

			IncrementalValuesProvider<MetadataReader> metadataReader = moduleMetadata
				.Select(static (metadata, _) =>
					metadata.GetMetadataReader());
			
			// Find all test cases (some of which may need generated facts)
			IncrementalValuesProvider<TestCases> testCases = metadataReader
				.Select(static (reader, cancellationToken) =>
					FindTestCases(reader, cancellationToken));

			// Find already-generated test types
			IncrementalValuesProvider<INamedTypeSymbol?> existingTestTypes = context.SyntaxProvider.CreateSyntaxProvider(
				static (node, cancellationToken) => {
					if (node is not ClassDeclarationSyntax classSyntax)
						return false;

					var typeFullName = GetFullName(classSyntax);

					// Ignore types not in the testcase namespace or that don't end with "Tests"
					if (!typeFullName.StartsWith (TestClassGenerator.TestNamespace))
						return false;
					if (!typeFullName.EndsWith ("Tests"))
						return false;

					return true;
				},
				static (generatorSyntaxContext, cancellationToken) => {
					var node = generatorSyntaxContext.Node;
					return generatorSyntaxContext.SemanticModel.GetDeclaredSymbol (node, cancellationToken) is INamedTypeSymbol typeSymbol
						? typeSymbol
						: null;
				});

			// Find already-generated test cases
			IncrementalValueProvider<TestCases> existingTestCases = existingTestTypes
				// Find test methods
				.SelectMany(static (typeSymbol, cancellationToken) => 
					FindExistingTestCases(typeSymbol, cancellationToken))
				.Collect()
				.Select(static (existingTestCases, cancellationToken) => {
					var testCases = new TestCases();
					foreach (var (suiteName, testName) in existingTestCases) {
						testCases.Add(suiteName, testName);
					}
					return testCases;
				});

			// Find the new test cases that need generated facts
			IncrementalValuesProvider<(string suiteName, IEnumerable<string> newCases, bool newTestSuite)> newTestCases = testCases
				.Combine(existingTestCases)
				.SelectMany(static (combined, cancellationToken) => {
					var (testCases, existingTestCases) = combined;
					return FindNewTestCases(testCases, existingTestCases, cancellationToken);
				});


			// Generate facts for all testcases that don't already exist
			context.RegisterSourceOutput(newTestCases, static (sourceProductionContext, newTestCases) => {
				StringBuilder sourceBuilder = new ();
				var suiteName = newTestCases.suiteName;
				bool newTestSuite = newTestCases.newTestSuite;

				sourceBuilder.Append (TestClassGenerator.GenerateClassHeader (suiteName, newTestSuite));
				foreach (var testCase in newTestCases.newCases)
					sourceBuilder.Append (TestClassGenerator.GenerateFact (testCase));
				sourceBuilder.Append (TestClassGenerator.GenerateClassFooter ());

				sourceProductionContext.AddSource ($"{suiteName}Tests.g.cs", sourceBuilder.ToString ());
			});

			static IEnumerable<(string SuiteName, IEnumerable<string> NewCases, bool newTestSuite)> FindNewTestCases (TestCases testCases, TestCases existingTestCases, CancellationToken cancellationToken) {
				foreach (var kvp in testCases.Suites) {
					cancellationToken.ThrowIfCancellationRequested ();

					string suiteName = kvp.Key;
					var cases = kvp.Value;

					bool newTestSuite = !existingTestCases.Suites.TryGetValue (suiteName, out HashSet<string> existingCases);
					var newCases = newTestSuite ? cases : cases.Except (existingCases);
					// Skip generating a test class if all testcases in the suite already exist.
					if (!newCases.Any ())
						continue;

					yield return (suiteName, newCases, newTestSuite);
				}
			}

			static IEnumerable<(string SuiteName, string TestName)> FindExistingTestCases (INamedTypeSymbol? typeSymbol, CancellationToken cancellationToken) {
				if (typeSymbol is null)
					yield break;

				var displayFormat = new SymbolDisplayFormat (
					typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
				string typeFullName = typeSymbol.ToDisplayString (displayFormat);

				// Ignore types not in the testcase namespace or that don't end with "Tests"

				string suiteName = typeFullName.Substring (TestClassGenerator.TestNamespace.Length + 1);
				suiteName = suiteName.Substring (0, suiteName.Length - "Tests".Length);
				foreach (var member in typeSymbol.GetMembers ()) {
					cancellationToken.ThrowIfCancellationRequested ();

					if (member is not IMethodSymbol methodSymbol)
						continue;
					yield return (suiteName, methodSymbol.Name);
				}
			}

			static TestCases FindTestCases (MetadataReader metadataReader, CancellationToken cancellationToken) {
				TestCases testCases = new ();
				string suiteName;

				// Find test classes to generate
				foreach (var typeDefHandle in metadataReader.TypeDefinitions) {
					cancellationToken.ThrowIfCancellationRequested ();

					TypeDefinition typeDef = metadataReader.GetTypeDefinition (typeDefHandle);
					// Must not be a nested type
					if (typeDef.IsNested)
						continue;

					string ns = metadataReader.GetString (typeDef.Namespace);
					// Must be in the testcases namespace
					if (!ns.StartsWith (TestCaseAssembly))
						continue;

					// Must have a Main method
					bool hasMain = false;
					foreach (var methodDefHandle in typeDef.GetMethods ()) {
						cancellationToken.ThrowIfCancellationRequested ();

						MethodDefinition methodDef = metadataReader.GetMethodDefinition (methodDefHandle);
						if (metadataReader.GetString (methodDef.Name) == "Main") {
							hasMain = true;
							break;
						}
					}
					if (!hasMain)
						continue;

					string testName = metadataReader.GetString (typeDef.Name);
					suiteName = ns.Substring (TestCaseAssembly.Length + 1);

					testCases.Add (suiteName, testName);
				}

				return testCases;
			}
		}
	}
}