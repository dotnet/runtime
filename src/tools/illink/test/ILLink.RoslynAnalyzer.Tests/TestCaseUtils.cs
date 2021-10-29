// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public abstract class TestCaseUtils
	{
		private static readonly string MonoLinkerTestsCases = "Mono.Linker.Tests.Cases";

		public static readonly ReferenceAssemblies Net6PreviewAssemblies =
			new ReferenceAssemblies (
				"net6.0",
				new PackageIdentity ("Microsoft.NETCore.App.Ref", "6.0.0-preview.7.21368.2"),
				Path.Combine ("ref", "net6.0"))
			.WithNuGetConfigFilePath (Path.Combine (TestCaseUtils.GetRepoRoot (), "NuGet.config"));

		private static ImmutableArray<MetadataReference> s_net6Refs;
		public async static ValueTask<ImmutableArray<MetadataReference>> GetNet6References ()
		{
			if (s_net6Refs.IsDefault) {
				var refs = await Net6PreviewAssemblies.ResolveAsync (null, default);
				ImmutableInterlocked.InterlockedInitialize (ref s_net6Refs, refs);
			}
			return s_net6Refs;
		}

		public static IEnumerable<object[]> GetTestData (string testSuiteName)
			=> s_testCases[testSuiteName].Keys.Select (testName => new object[] { testName });

		public static void RunTest (string suiteName, string testName, (string, string)[] MSBuildProperties, Func<TestCase, bool>? shouldRun = null)
		{
			var testCase = s_testCases[suiteName][testName];
			if (shouldRun == null || shouldRun (testCase))
				testCase.Run (MSBuildProperties);
		}

		private static readonly Dictionary<string, Dictionary<string, TestCase>> s_testCases = InitializeTestCases ();

		private static Dictionary<string, Dictionary<string, TestCase>> InitializeTestCases ()
		{
			var testCases = new Dictionary<string, Dictionary<string, TestCase>> ();
			foreach (var file in GetTestFiles ()) {
				// Some tests are in nested directories. Walk up until we get the test suite directory.
				string directory = Path.GetDirectoryName (file)!;
				string parentDirectory;
				while (Path.GetFileName (parentDirectory = Path.GetDirectoryName (directory)!) != MonoLinkerTestsCases)
					directory = parentDirectory;
				string suiteName = Path.GetFileName (directory);

				if (!testCases.TryGetValue (suiteName, out var suiteTestCases)) {
					suiteTestCases = new ();
					testCases.Add (suiteName, suiteTestCases);
				}

				foreach (var testCase in BuildTestCasesForFile (file)) {
					var canditateTestName = GetMemberSyntaxFullName (testCase.MemberSyntax);
					string testName = canditateTestName;
					int index = 0;
					while (!suiteTestCases.TryAdd (testName, testCase)) {
						testName = canditateTestName + "#" + (++index).ToString ();
					}

					testCase.Name = testName;
				}
			}
			return testCases;
		}

		private static IEnumerable<TestCase> BuildTestCasesForFile (string testFile)
		{
			var root = CSharpSyntaxTree.ParseText (File.ReadAllText (testFile)).GetRoot ();
			foreach (var node in root.DescendantNodes ()) {
				if (node is MemberDeclarationSyntax m) {
					var attrs = m.AttributeLists.SelectMany (al => al.Attributes.Where (IsWellKnown)).ToList ();
					if (attrs.Count > 0) {
						yield return new TestCase (m, attrs);
					}
				}
			}

			static bool IsWellKnown (AttributeSyntax attr)
			{
				switch (attr.Name.ToString ()) {
				// Currently, the analyzer's test infra only understands these attributes when placed on methods.
				case "ExpectedWarning":
				case "LogContains":
				case "LogDoesNotContain":
				case "UnrecognizedReflectionAccessPattern":
					return true;
				}

				return false;
			}
		}

		public static void GetDirectoryPaths (out string rootSourceDirectory, out string testAssemblyPath)
		{
#if DEBUG
			var configDirectoryName = "Debug";
#else
			var configDirectoryName = "Release";
#endif

#if NET6_0
			const string tfm = "net6.0";
#else
			const string tfm = "net5.0";
#endif

			// Working directory is artifacts/bin/Mono.Linker.Tests/<config>/<tfm>
			var artifactsBinDir = Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..");
			rootSourceDirectory = Path.GetFullPath (Path.Combine (artifactsBinDir, "..", "..", "test", "Mono.Linker.Tests.Cases"));
			testAssemblyPath = Path.GetFullPath (Path.Combine (artifactsBinDir, "ILLink.RoslynAnalyzer.Tests", configDirectoryName, tfm));
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
				Assert.True (false, "Unsupported expr kind " + expr.Kind ());
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

		public static IEnumerable<string> GetTestFiles ()
		{
			GetDirectoryPaths (out var rootSourceDir, out _);
			foreach (var subDir in Directory.EnumerateDirectories (rootSourceDir, "*", SearchOption.AllDirectories)) {
				var subDirName = Path.GetFileName (subDir);
				switch (subDirName) {
				case "bin":
				case "obj":
				case "Properties":
				case "Dependencies":
				case "Individual":
					continue;
				}

				foreach (var file in Directory.EnumerateFiles (subDir, "*.cs")) {
					yield return file;
				}
			}
		}

		public static (string, string)[] UseMSBuildProperties (params string[] MSBuildProperties)
		{
			return MSBuildProperties.Select (msbp => ($"build_property.{msbp}", "true")).ToArray ();
		}

		public static string GetRepoRoot ()
		{
			return Directory.GetParent (ThisFile ())!.Parent!.Parent!.FullName;

			string ThisFile ([CallerFilePath] string path = "") => path;
		}

		public static string GetMemberSyntaxFullName (MemberDeclarationSyntax member)
		{
			StringBuilder fullName = new ();
			var parent = member.Parent;
			while (parent is ClassDeclarationSyntax parentClass) {
				fullName.Insert (0, ".");
				fullName.Insert (0, parentClass.Identifier.ToString ());
				parent = parentClass.Parent;
			}

			fullName.Append (GetMemberSyntaxName (member));
			return fullName.ToString ();
		}

		public static string GetMemberSyntaxName (MemberDeclarationSyntax member) =>
			member switch {
				MethodDeclarationSyntax method => method.Identifier.ToString (),
				PropertyDeclarationSyntax property => property.Identifier.ToString (),
				FieldDeclarationSyntax field => field.Declaration.Variables.Single ().Identifier.ToString (),
				EventDeclarationSyntax @event => @event.Identifier.ToString (),
				ClassDeclarationSyntax @class => @class.Identifier.ToString (),
				ConstructorDeclarationSyntax ctor => ctor.Modifiers.Any (t => t.Text == "static") ? ".cctor" : ".ctor",
				_ => "UnknownMember"
			};
	}
}
