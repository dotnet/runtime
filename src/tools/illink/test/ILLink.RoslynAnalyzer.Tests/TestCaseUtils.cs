// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using SourceGenerators.Tests;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public abstract class TestCaseUtils
	{
		private static readonly string MonoLinkerTestsCases = "Mono.Linker.Tests.Cases";

		public static string FindTestSuiteDir (string rootDir, string suiteName)
		{
			string[] suiteParts = suiteName.Split ('.');
			string currentDir = rootDir;
			foreach (var suitePart in suiteParts) {
				string dirCandidate = Path.Combine (currentDir, suitePart);
				if (currentDir == rootDir || Directory.Exists (dirCandidate))
					currentDir = dirCandidate;
				else
					currentDir += $".{suitePart}";
			}

			return currentDir;
		}

		public static async Task RunTestFile (string suiteName, string testName, bool allowMissingWarnings, params (string, string)[] msbuildProperties)
		{
			GetDirectoryPaths (out string rootSourceDir);
			Debug.Assert (Path.GetFileName (rootSourceDir) == MonoLinkerTestsCases);
			var testSuiteDir = FindTestSuiteDir (rootSourceDir, suiteName);
			Assert.True (Directory.Exists (testSuiteDir));
			var testCaseDir = Path.Combine (testSuiteDir, testName);
			string testPath;
			if (Directory.Exists (testCaseDir)) {
				testPath = Path.Combine (testCaseDir, $"Program.cs");
			} else {
				testCaseDir = testSuiteDir;
				testPath = Path.Combine (testSuiteDir, $"{testName}.cs");
			}
			Assert.True (File.Exists (testPath));
			var tree = SyntaxFactory.ParseSyntaxTree (
				SourceText.From (File.OpenRead (testPath), Encoding.UTF8),
				path: testPath);

			var testDependenciesSource = GetTestDependencies (testCaseDir, tree)
				.Where (f => Path.GetExtension (f) == ".cs")
				.Select (f => SyntaxFactory.ParseSyntaxTree (SourceText.From (File.OpenRead (f))));
			var additionalFiles = GetAdditionalFiles (rootSourceDir, tree);

			var (comp, model, exceptionDiagnostics) = TestCaseCompilation.CreateCompilation (
					tree,
					consoleApplication: false,
					msbuildProperties,
					additionalSources: testDependenciesSource,
					additionalFiles: additionalFiles);

			// Note that the exception diagnostics will be empty until the analyzer has run,
			// so be sure to get them after awaiting GetAnalyzerDiagnosticsAsync().
			var diags = (await comp.GetAnalyzerDiagnosticsAsync ()).AddRange (exceptionDiagnostics);

			var testChecker = new TestChecker ((CSharpSyntaxTree) tree, model, diags);
			testChecker.Check (allowMissingWarnings);
		}

		private static IEnumerable<string> GetTestDependencies (string testCaseDir, SyntaxTree testSyntaxTree)
		{
			foreach (var attribute in testSyntaxTree.GetRoot ().DescendantNodes ().OfType<AttributeSyntax> ()) {
				var attributeName = attribute.Name.ToString ();
				if (attributeName != "SetupCompileBefore" && attributeName != "SandboxDependency")
					continue;
				var args = LinkerTestBase.GetAttributeArguments (attribute);

				switch (attributeName) {
				case "SetupCompileBefore": {
						var arrayExpression = args["#1"];
						if (arrayExpression is not (ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax))
							throw new InvalidOperationException ();
						foreach (var sourceFile in args["#1"].DescendantNodes ().OfType<LiteralExpressionSyntax> ())
							yield return Path.Combine (testCaseDir, LinkerTestBase.GetStringFromExpression (sourceFile));
						break;
					}
				case "SandboxDependency": {
						var argExpression = args["#0"];
						string sourceFile;
						if (argExpression is TypeOfExpressionSyntax typeOfSyntax) {
							// If the argument is a Type, assume the dependency is located in a file with the
							// outermost declaring type's name in the Dependencies subdirectory.
							var typeNameSyntax = typeOfSyntax.Type;
							while (typeNameSyntax is QualifiedNameSyntax qualifiedNameSyntax)
								typeNameSyntax = qualifiedNameSyntax.Left;
							sourceFile = Path.Combine ("Dependencies", $"{typeNameSyntax.ToString ()}.cs");
						} else {
							sourceFile = LinkerTestBase.GetStringFromExpression (args["#0"]);
						}
						if (!sourceFile.EndsWith (".cs"))
							throw new NotSupportedException ();
						yield return Path.Combine (testCaseDir, sourceFile);
						break;
					}
				default:
					throw new InvalidOperationException ();
				}
			}
		}

		private static IEnumerable<AdditionalText> GetAdditionalFiles (string rootSourceDir, SyntaxTree tree)
		{
			var resolver = new XmlFileResolver (rootSourceDir);
			foreach (var attribute in tree.GetRoot ().DescendantNodes ().OfType<AttributeSyntax> ()) {
				switch (attribute.Name.ToString ()) {
				case nameof (SetupLinkAttributesFile):
					break;
				case nameof (SetupCompileResourceAttribute):
					var args = attribute.ArgumentList?.Arguments;
					if (args?.Count == 2 && args?[1].ToString () == "\"ILLink.LinkAttributes.xml\"")
						break;
					continue;
				default:
					continue;
				}

				var xmlFileName = attribute.ArgumentList?.Arguments[0].ToString ().Trim ('"') ?? "";
				var resolvedPath = resolver.ResolveReference (xmlFileName, rootSourceDir);
				if (resolvedPath != null) {
					var stream = resolver.OpenRead (resolvedPath);
					XmlText text = new ("ILLink.LinkAttributes.xml", stream);
					yield return text;
				}
			}
		}

		public static void GetDirectoryPaths (out string rootSourceDirectory)
		{
			string linkerTestDirectory = (string)AppContext.GetData("ILLink.RoslynAnalyzer.Tests.LinkerTestDir")!;
			rootSourceDirectory = Path.GetFullPath(Path.Combine(linkerTestDirectory, MonoLinkerTestsCases));
		}

		// Accepts typeof expressions, with a format specifier
		public static string GetStringFromExpression (TypeOfExpressionSyntax expr, SemanticModel semanticModel, SymbolDisplayFormat displayFormat)
		{
			var typeSymbol = semanticModel.GetSymbolInfo (expr.Type).Symbol;
			return typeSymbol?.ToDisplayString (displayFormat) ?? throw new InvalidOperationException ();
		}

		// Accepts string literal expressions or binary expressions concatenating strings
		public static string GetStringFromExpression (ExpressionSyntax expr, SemanticModel? semanticModel = null)
		{
			if (expr == null)
				return null!;

			switch (expr.Kind ()) {
			case SyntaxKind.AddExpression:
				var addExpr = (BinaryExpressionSyntax) expr;
				return GetStringFromExpression (addExpr.Left, semanticModel) + GetStringFromExpression (addExpr.Right, semanticModel);

			case SyntaxKind.InvocationExpression:
				var nameofValue = semanticModel!.GetConstantValue (expr);
				if (nameofValue.HasValue)
					return (nameofValue.Value as string)!;

				return string.Empty;

			case SyntaxKind.StringLiteralExpression:
				var strLiteral = (LiteralExpressionSyntax) expr;
				var token = strLiteral.Token;
				Assert.Equal (SyntaxKind.StringLiteralToken, token.Kind ());
				return token.ValueText;

			default:
				Assert.Fail("Unsupported expr kind " + expr.Kind ());
				return null!;
			}
		}

		public static Dictionary<string, ExpressionSyntax> GetAttributeArguments (AttributeSyntax attribute)
		{
			Dictionary<string, ExpressionSyntax> arguments = new Dictionary<string, ExpressionSyntax> ();
			int ordinal = 0;
			foreach (var argument in attribute.ArgumentList!.Arguments) {
				string argName;
				if (argument.NameEquals != null) {
					argName = argument.NameEquals.ChildNodes ().OfType<IdentifierNameSyntax> ().First ().Identifier.ValueText;
				} else if (argument.NameColon is NameColonSyntax nameColon) {
					argName = nameColon.Name.Identifier.ValueText;
				} else {
					argName = "#" + ordinal.ToString ();
					ordinal++;
				}
				arguments.Add (argName, argument.Expression);
			}

			return arguments;
		}

		public static (string, string)[] UseMSBuildProperties (params string[] MSBuildProperties)
		{
			return MSBuildProperties.Select (msbp => ($"build_property.{msbp}", "true")).ToArray ();
		}
	}
}
