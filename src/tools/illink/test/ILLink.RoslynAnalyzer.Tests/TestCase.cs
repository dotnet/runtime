// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class TestCase
	{
		public readonly MemberDeclarationSyntax MemberSyntax;

		private readonly IEnumerable<AttributeSyntax> Attributes;

		public string? Name { get; set; }

		public TestCase (MemberDeclarationSyntax memberSyntax, IEnumerable<AttributeSyntax> attributes)
		{
			MemberSyntax = memberSyntax;
			Attributes = attributes;
		}

		public void Run (params (string, string)[] MSBuildProperties)
		{
			var testSyntaxTree = MemberSyntax.SyntaxTree.GetRoot ().SyntaxTree;
			var testDependenciesSource = GetTestDependencies (testSyntaxTree)
				.Select (testDependency => CSharpSyntaxTree.ParseText (File.ReadAllText (testDependency)));

			var test = new TestChecker (
				MemberSyntax,
				TestCaseCompilation.CreateCompilation (
					testSyntaxTree,
					MSBuildProperties,
					additionalSources: testDependenciesSource).Result);

			test.ValidateAttributes (Attributes);
		}

		private static IEnumerable<string> GetTestDependencies (SyntaxTree testSyntaxTree)
		{
			TestCaseUtils.GetDirectoryPaths (out var rootSourceDir, out _);
			foreach (var attribute in testSyntaxTree.GetRoot ().DescendantNodes ().OfType<AttributeSyntax> ()) {
				if (attribute.Name.ToString () != "SetupCompileBefore")
					continue;

				var testNamespace = testSyntaxTree.GetRoot ().DescendantNodes ().OfType<NamespaceDeclarationSyntax> ().Single ().Name.ToString ();
				var testSuiteName = testNamespace.Substring (testNamespace.LastIndexOf ('.') + 1);
				var args = TestCaseUtils.GetAttributeArguments (attribute);
				foreach (var sourceFile in ((ImplicitArrayCreationExpressionSyntax) args["#1"]).DescendantNodes ().OfType<LiteralExpressionSyntax> ())
					yield return Path.Combine (rootSourceDir, testSuiteName, TestCaseUtils.GetStringFromExpression (sourceFile));
			}
		}
	}
}
