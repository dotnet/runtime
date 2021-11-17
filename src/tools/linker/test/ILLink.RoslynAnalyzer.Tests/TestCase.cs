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

		public void Run ((CompilationWithAnalyzers, SemanticModel) compAndModel)
		{
			var testSyntaxTree = MemberSyntax.SyntaxTree;
			var testDependenciesSource = GetTestDependencies (testSyntaxTree)
				.Select (testDependency => CSharpSyntaxTree.ParseText (File.ReadAllText (testDependency)));

			var test = new TestChecker (MemberSyntax, compAndModel);
			test.ValidateAttributes (Attributes);
		}

		public static IEnumerable<string> GetTestDependencies (SyntaxTree testSyntaxTree)
		{
			LinkerTestBase.GetDirectoryPaths (out var rootSourceDir, out _);
			foreach (var attribute in testSyntaxTree.GetRoot ().DescendantNodes ().OfType<AttributeSyntax> ()) {
				if (attribute.Name.ToString () != "SetupCompileBefore")
					continue;

				var testNamespace = testSyntaxTree.GetRoot ().DescendantNodes ().OfType<NamespaceDeclarationSyntax> ().Single ().Name.ToString ();
				var testSuiteName = testNamespace.Substring (testNamespace.LastIndexOf ('.') + 1);
				var args = LinkerTestBase.GetAttributeArguments (attribute);
				foreach (var sourceFile in ((ImplicitArrayCreationExpressionSyntax) args["#1"]).DescendantNodes ().OfType<LiteralExpressionSyntax> ())
					yield return Path.Combine (rootSourceDir, testSuiteName, LinkerTestBase.GetStringFromExpression (sourceFile));
			}
		}
	}
}
