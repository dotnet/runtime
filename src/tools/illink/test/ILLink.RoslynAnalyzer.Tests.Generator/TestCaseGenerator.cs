// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
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
}}
";
		}
	}

	[Generator]
	public class TestCaseGenerator : IIncrementalGenerator
	{
		public const string TestCaseAssembly = "Mono.Linker.Tests.Cases";

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

			IncrementalValueProvider<AnalyzerConfigOptionsProvider> options = context.AnalyzerConfigOptionsProvider;

			// For any steps below which can fail (for example, getting metadata might return null),
			// we use SelectMany to return zero or one results.
			IncrementalValuesProvider<(IAssemblySymbol, string?)> assemblyAndSuite = metadataReferences
				.Combine(compilation)
				.Combine(options)
				.SelectMany(static (referenceAndCompilationAndOptions, _) => {
					var (referenceAndCompilation, options) = referenceAndCompilationAndOptions;
					var (reference, compilation) = referenceAndCompilation;
					if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol asmSym)
						return ImmutableArray<(IAssemblySymbol, string?)>.Empty;
					if (!options.GlobalOptions.TryGetValue("build_property.TestCaseBuildOutputRoot", out var testCaseBuildOutputRootValue))
						throw new Exception("Missing build property TestCaseBuildOutputRoot");
					var testCaseBuildOutputRoot = Path.GetFullPath(testCaseBuildOutputRootValue);
					var referenceDirectory = Path.GetDirectoryName (reference.Display);
					if (!referenceDirectory.StartsWith(testCaseBuildOutputRoot))
						return ImmutableArray<(IAssemblySymbol, string?)>.Empty;
					var suiteName = referenceDirectory.Length < testCaseBuildOutputRoot.Length + 1
						? null
						: Path.GetDirectoryName (referenceDirectory.Substring (testCaseBuildOutputRoot.Length + 1));
					return ImmutableArray.Create((asmSym, suiteName));
				});

			IncrementalValuesProvider<(AssemblyMetadata, string?)> metadataAndSuite = assemblyAndSuite
				.SelectMany (static (combined, _) => {
					var (asmSym, suite) = combined;
					return asmSym.GetMetadata() is AssemblyMetadata metadata
						? ImmutableArray.Create ((metadata, suite))
						: ImmutableArray<(AssemblyMetadata, string?)>.Empty;
				});

			IncrementalValuesProvider<TestCases> testCasesPerAssembly = metadataAndSuite
				.Select (static (combined, cancellationToken) => {
					var (asmMetadata, suite) = combined;
					ModuleMetadata moduleMetadata = asmMetadata.GetModules().Single();
					MetadataReader metadataReader = moduleMetadata.GetMetadataReader();
					return FindTestCases (suite, metadataReader, cancellationToken);
				});

			IncrementalValueProvider<TestCases> testCases = testCasesPerAssembly
				.Collect ()
				.Select (static (testCasesPerAssembly, cancellationToken) => {
					var testCases = new TestCases();
					foreach (var assemblyTestCases in testCasesPerAssembly) {
						foreach (var kvp in assemblyTestCases.Suites) {
							var suiteName = kvp.Key;
							var caseNames = kvp.Value;
							foreach (var caseName in caseNames)
								testCases.Add(suiteName, caseName);
						}
					}
					return testCases;
				});

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
					TestCases testCases = new ();
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

			static TestCases FindTestCases (string? explicitSuiteName, MetadataReader metadataReader, CancellationToken cancellationToken) {
				TestCases testCases = new ();

				bool topLevelStatementsAssembly = false;

				// Find test classes to generate
				foreach (var typeDefHandle in metadataReader.TypeDefinitions) {
					cancellationToken.ThrowIfCancellationRequested ();

					TypeDefinition typeDef = metadataReader.GetTypeDefinition (typeDefHandle);
					// Must not be a nested type
					if (typeDef.IsNested)
						continue;

					string typeName = metadataReader.GetString (typeDef.Name);
					
					// Allow top-level statements with generated Program class
					bool compilerGeneratedProgramType = false;
					if (typeName == "Program")  {
						// Look for CompilerGeneratedCodeAttribute
						foreach (var caHandle in typeDef.GetCustomAttributes ()) {
							var ca = metadataReader.GetCustomAttribute (caHandle);
							if (ca.Constructor.Kind is not HandleKind.MemberReference)
								continue;
							var caCtor = metadataReader.GetMemberReference ((MemberReferenceHandle) ca.Constructor);
							var caType = metadataReader.GetTypeReference ((TypeReferenceHandle) caCtor.Parent);
							if (metadataReader.GetString (caType.Name) == "CompilerGeneratedAttribute")
								compilerGeneratedProgramType = true;
						}
					}

					string? ns = null;
					if (!compilerGeneratedProgramType) {
						ns = metadataReader.GetString (typeDef.Namespace);
						// Must be in the testcases namespace
						if (!ns.StartsWith (TestCaseAssembly))
							continue;
					}

					// Must have a Main method
					bool hasMain = false;
					foreach (var methodDefHandle in typeDef.GetMethods ()) {
						cancellationToken.ThrowIfCancellationRequested ();

						MethodDefinition methodDef = metadataReader.GetMethodDefinition (methodDefHandle);
						if (metadataReader.GetString (methodDef.Name) == (compilerGeneratedProgramType ? "<Main>$" : "Main")) {
							hasMain = true;
							break;
						}
					}
					if (!hasMain)
						continue;

					if (topLevelStatementsAssembly)
						throw new NotImplementedException ("Multiple test cases in an assembly with top-level statements is not supported.");

					string testName;
					string suiteName;

					if (compilerGeneratedProgramType) {
						topLevelStatementsAssembly = true;
						if (explicitSuiteName == null)
							throw new InvalidOperationException ("No suite name supplied for compiler-generated Program type.");
						testName = metadataReader.GetString (metadataReader.GetAssemblyDefinition ().Name);
						suiteName = explicitSuiteName;
					} else {
						if (explicitSuiteName != null)
							throw new InvalidOperationException ($"Suite name should be determined from namespace, but explicit suite '{explicitSuiteName}' was supplied.");
						suiteName = ns!.Substring (TestCaseAssembly.Length + 1);						
						testName = typeName;
					}

					testCases.Add (suiteName, testName);
				}
				return testCases;
			}
		}
	}
}