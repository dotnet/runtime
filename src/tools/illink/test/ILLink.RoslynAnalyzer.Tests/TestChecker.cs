// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	internal class TestChecker
	{
		private readonly CompilationWithAnalyzers Compilation;

		private readonly SemanticModel SemanticModel;

		private readonly List<(string Id, string Message)> DiagnosticMessages;

		public TestChecker (SyntaxNode syntaxNode, (CompilationWithAnalyzers Compilation, SemanticModel SemanticModel) compilationResult)
		{
			Compilation = compilationResult.Compilation;
			SemanticModel = compilationResult.SemanticModel;
			DiagnosticMessages = Compilation.GetAnalyzerDiagnosticsAsync ().Result
				.Where (d => d.Location.SourceSpan.IntersectsWith (syntaxNode.Span))
				.Select (d => (d.Id, d.GetMessage ()))
				.ToList ();
		}

		public void ValidateAttributes (List<AttributeSyntax> attributes)
		{
			foreach (var attribute in attributes) {
				switch (attribute.Name.ToString ()) {
				case "ExpectedWarning":
					ValidateExpectedWarningAttribute (attribute!);
					break;

				case "LogContains":
					ValidateLogContainsAttribute (attribute!);
					break;

				case "LogDoesNotContain":
					ValidateLogDoesNotContainAttribute (attribute!);
					break;

				case "UnrecognizedReflectionAccessPattern":
					ValidateUnrecognizedReflectionAccessPatternAttribute (attribute!);
					break;
				}
			}
		}

		private void ValidateExpectedWarningAttribute (AttributeSyntax attribute)
		{
			var args = TestCaseUtils.GetAttributeArguments (attribute);
			string expectedWarningCode = TestCaseUtils.GetStringFromExpression (args["#0"]);

			if (!expectedWarningCode.StartsWith ("IL"))
				return;

			if (args.TryGetValue ("GlobalAnalysisOnly", out var globalAnalysisOnly) &&
				globalAnalysisOnly is LiteralExpressionSyntax { Token: { Value: true } })
				return;

			List<string> expectedMessages = args
				.Where (arg => arg.Key.StartsWith ("#") && arg.Key != "#0")
				.Select (arg => TestCaseUtils.GetStringFromExpression (arg.Value))
				.ToList ();

			Assert.True (
				DiagnosticMessages.Any (mc => {
					if (mc.Id != expectedWarningCode)
						return true;

					foreach (var expectedMessage in expectedMessages)
						if (!mc.Message.Contains (expectedMessage))
							return false;

					return true;
				}),
					$"Expected to find warning containing:{string.Join (" ", expectedMessages.Select (m => "'" + m + "'"))}" +
					$", but no such message was found.{ Environment.NewLine}In diagnostics: {string.Join (Environment.NewLine, DiagnosticMessages)}");
		}

		private void ValidateLogContainsAttribute (AttributeSyntax attribute)
		{
			var arg = Assert.Single (TestCaseUtils.GetAttributeArguments (attribute));
			var text = TestCaseUtils.GetStringFromExpression (arg.Value);

			// If the text starts with `warning IL...` then it probably follows the pattern
			//	'warning <diagId>: <location>:'
			// We don't want to repeat the location in the error message for the analyzer, so
			// it's better to just trim here. We've already filtered by diagnostic location so
			// the text location shouldn't matter
			if (text.StartsWith ("warning IL")) {
				var firstColon = text.IndexOf (": ");
				if (firstColon > 0) {
					var secondColon = text.IndexOf (": ", firstColon + 1);
					if (secondColon > 0) {
						text = text.Substring (secondColon + 2);
					}
				}
			}

			Assert.True (DiagnosticMessages.Any (d => d.Message.Contains (text)),
				$"Could not find text:\n{text}\nIn diagnostics:\n{(string.Join (Environment.NewLine, DiagnosticMessages))}");
		}

		private void ValidateLogDoesNotContainAttribute (AttributeSyntax attribute)
		{
			var arg = Assert.Single (TestCaseUtils.GetAttributeArguments (attribute));
			var text = TestCaseUtils.GetStringFromExpression (arg.Value);
			foreach (var diagnostic in DiagnosticMessages)
				Assert.DoesNotContain (text, diagnostic.Message);
		}

		private void ValidateUnrecognizedReflectionAccessPatternAttribute (AttributeSyntax attribute)
		{
			var args = TestCaseUtils.GetAttributeArguments (attribute);

			MemberDeclarationSyntax sourceMember = attribute.Ancestors ().OfType<MemberDeclarationSyntax> ().First ();
			if (SemanticModel.GetDeclaredSymbol (sourceMember) is not ISymbol memberSymbol)
				return;

			string sourceMemberName = memberSymbol!.GetDisplayName ();
			string expectedReflectionMemberMethodType = TestCaseUtils.GetStringFromExpression (args["#0"], SemanticModel);
			string expectedReflectionMemberMethodName = TestCaseUtils.GetStringFromExpression (args["#1"], SemanticModel);

			ArrayCreationExpressionSyntax reflectionMethodParametersExpr = (ArrayCreationExpressionSyntax) (args["#2"] ?? args["reflectionMethodParameters"]);
			var reflectionMethodParameters = new List<string> ();
			if (reflectionMethodParametersExpr != null) {
				foreach (var rmp in reflectionMethodParametersExpr.Initializer!.Expressions)
					reflectionMethodParameters.Add (TestCaseUtils.GetStringFromExpression (rmp, SemanticModel));
			}

			ArrayCreationExpressionSyntax messageExpr = (ArrayCreationExpressionSyntax) (args["#3"] ?? args["message"]);
			var expectedStringsInMessage = new List<string> ();
			if (messageExpr != null)
				foreach (var m in messageExpr.Initializer!.Expressions)
					expectedStringsInMessage.Add (TestCaseUtils.GetStringFromExpression (m, SemanticModel));

			string expectedWarningCode = string.Empty;
			if (args.TryGetValue ("#4", out var messageCodeExpr) || args.TryGetValue ("messageCode", out messageCodeExpr)) {
				expectedWarningCode = TestCaseUtils.GetStringFromExpression (messageCodeExpr);
				Assert.True (expectedWarningCode.StartsWith ("IL"),
					$"The warning code specified in {messageCodeExpr.ToString ()} must start with the 'IL' prefix. Specified value: '{expectedWarningCode}'");
			}

			string expectedReturnType = string.Empty;
			if (args.TryGetValue ("#5", out var returnTypeExpr) || args.TryGetValue ("returnType", out returnTypeExpr))
				expectedReturnType = TestCaseUtils.GetStringFromExpression (returnTypeExpr, SemanticModel);

			var sb = new StringBuilder ();
			if (!string.IsNullOrEmpty (expectedReturnType))
				sb.Append (expectedReturnType).Append (" ");

			sb.Append (expectedReflectionMemberMethodType).Append ("::").Append (expectedReflectionMemberMethodName);
			if (!expectedReflectionMemberMethodName.EndsWith (".get") &&
				!expectedReflectionMemberMethodName.EndsWith (".set") &&
				reflectionMethodParameters is not null)
				sb.Append ("(").Append (string.Join (",", reflectionMethodParameters)).Append (")");

			var reflectionAccessPattern = sb.ToString ();

			Assert.True (
				DiagnosticMessages.Any (mc => {
					if (!string.IsNullOrEmpty (expectedWarningCode) && mc.Id != expectedWarningCode)
						return false;

					if (!mc.Message.Contains (sourceMemberName))
						return false;

					foreach (var expectedString in expectedStringsInMessage)
						if (!mc.Message.Contains (expectedString))
							return false;

					return mc.Message.Contains (reflectionAccessPattern);
				}), $"Expected to find unrecognized reflection access pattern '{(expectedWarningCode == string.Empty ? "" : expectedWarningCode + " ")}" +
					$"{sourceMemberName}: Usage of {reflectionAccessPattern} unrecognized.");
		}
	}
}
