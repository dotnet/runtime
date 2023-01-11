// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.RoslynAnalyzer.Tests
{
	sealed class TestCases
	{
		// Maps from suite name to a set of testcase names.
		// Suite name is:
		// - The namespace of the test class, minus "Mono.Linker.Tests.Cases", for linker tests
		// - The namespace + test class name, minus "ILLink.RoslynAnalyzer.Tests", for analyzer tests
		// Testcase name is:
		// - The test class name, for linker tests
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
	public class TestCaseGenerator : ISourceGenerator
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

			// Find test classes
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

			TestCases existingTestCases = ((ExistingTestCaseDiscoverer) context.SyntaxContextReceiver!).ExistingTestCases;

			// Generate test facts
			foreach (var kvp in testCases.Suites) {
				suiteName = kvp.Key;
				var cases = kvp.Value;

				bool newTestSuite = !existingTestCases.Suites.TryGetValue (suiteName, out HashSet<string> existingCases);
				var newCases = newTestSuite ? cases : cases.Except (existingCases);
				// Skip generating a test class if all testcases in the suite already exist.
				if (!newCases.Any ())
					continue;

				StringBuilder sourceBuilder = new ();
				sourceBuilder.Append (TestClassGenerator.GenerateClassHeader (suiteName, newTestSuite));
				// Generate facts for all testcases that don't already exist
				foreach (var testCase in newCases)
					sourceBuilder.Append (TestClassGenerator.GenerateFact (testCase));
				sourceBuilder.Append (TestClassGenerator.GenerateClassFooter ());

				context.AddSource ($"{suiteName}Tests.g.cs", sourceBuilder.ToString ());
			}
		}

		public void Initialize (GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications (() => new ExistingTestCaseDiscoverer ());
		}
	}

	sealed class ExistingTestCaseDiscoverer : ISyntaxContextReceiver
	{
		public readonly TestCases ExistingTestCases = new TestCases ();

		public void OnVisitSyntaxNode (GeneratorSyntaxContext context)
		{
			if (context.Node is not ClassDeclarationSyntax classSyntax)
				return;

			if (context.SemanticModel.GetDeclaredSymbol (classSyntax) is not INamedTypeSymbol typeSymbol)
				return;

			var displayFormat = new SymbolDisplayFormat (
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
			string typeFullName = typeSymbol.ToDisplayString (displayFormat);

			// Ignore types not in the testcase namespace or that don't end with "Tests"
			if (!typeFullName.StartsWith (TestClassGenerator.TestNamespace))
				return;
			if (!typeFullName.EndsWith ("Tests"))
				return;

			string suiteName = typeFullName.Substring (TestClassGenerator.TestNamespace.Length + 1);
			suiteName = suiteName.Substring (0, suiteName.Length - 5);
			foreach (var memberSymbol in typeSymbol.GetMembers ()) {
				if (memberSymbol is not IMethodSymbol methodSymbol)
					continue;
				ExistingTestCases.Add (suiteName, methodSymbol.Name);
			}
		}
	}
}