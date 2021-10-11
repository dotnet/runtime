// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	internal class TestChecker
	{
		private readonly CompilationWithAnalyzers Compilation;

		private readonly SemanticModel SemanticModel;

		private readonly List<Diagnostic> DiagnosticMessages;

		private readonly SyntaxNode MemberSyntax;

		public TestChecker (MemberDeclarationSyntax memberSyntax, (CompilationWithAnalyzers Compilation, SemanticModel SemanticModel) compilationResult)
		{
			Compilation = compilationResult.Compilation;
			SemanticModel = compilationResult.SemanticModel;
			DiagnosticMessages = Compilation.GetAnalyzerDiagnosticsAsync ().Result
				.Where (d => {
					// Filter down to diagnostics which originate from this member.

					// Test data may include diagnostics originating from a testcase or testcase dependencies.
					if (memberSyntax.SyntaxTree != d.Location.SourceTree)
						return false;

					// Filter down to diagnostics which originate from this member
					if (memberSyntax is ClassDeclarationSyntax classSyntax) {
						if (SemanticModel.GetDeclaredSymbol (classSyntax) is not ITypeSymbol typeSymbol)
							throw new NotImplementedException ("Unable to get type symbol for class declaration syntax.");

						if (typeSymbol.Locations.Length != 1)
							throw new NotImplementedException ("Type defined in multiple source locations.");

						// For classes, only consider diagnostics which originate from the type (not its members).
						// Approximate this by getting the location from the start of the type's syntax (which includes
						// attributes declared on the type) to the opening brace.
						var classSpan = TextSpan.FromBounds (
							classSyntax.GetLocation ().SourceSpan.Start,
							classSyntax.OpenBraceToken.GetLocation ().SourceSpan.Start
						);

						return d.Location.SourceSpan.IntersectsWith (classSpan);
					}

					return d.Location.SourceSpan.IntersectsWith (memberSyntax.Span);
				})
				.ToList ();
			MemberSyntax = memberSyntax;
		}

		bool IsExpectedDiagnostic (AttributeSyntax attribute)
		{
			switch (attribute.Name.ToString ()) {
			case "ExpectedWarning":
				var args = TestCaseUtils.GetAttributeArguments (attribute);
				if (args.TryGetValue ("ProducedBy", out var producedBy) &&
					producedBy is MemberAccessExpressionSyntax memberAccessExpression &&
					memberAccessExpression.Name is IdentifierNameSyntax identifierNameSyntax &&
					identifierNameSyntax.Identifier.ValueText == "Trimmer")
					return false;
				return true;
			case "LogContains":
			case "UnrecognizedReflectionAccessPattern":
				return true;
			default:
				return false;
			}
		}

		bool TryValidateExpectedDiagnostic (AttributeSyntax attribute, List<Diagnostic> diagnostics, [NotNullWhen (true)] out int? matchIndex, [NotNullWhen (false)] out string? missingDiagnosticMessage)
		{
			switch (attribute.Name.ToString ()) {
			case "ExpectedWarning":
				return TryValidateExpectedWarningAttribute (attribute!, diagnostics, out matchIndex, out missingDiagnosticMessage);
			case "LogContains":
				return TryValidateLogContainsAttribute (attribute!, diagnostics, out matchIndex, out missingDiagnosticMessage);
			case "UnrecognizedReflectionAccessPattern":
				return TryValidateUnrecognizedReflectionAccessPatternAttribute (attribute!, diagnostics, out matchIndex, out missingDiagnosticMessage);
			default:
				throw new InvalidOperationException ($"Unsupported attribute type {attribute.Name}");
			}
		}

		public void ValidateAttributes (List<AttributeSyntax> attributes)
		{
			var unmatchedDiagnostics = DiagnosticMessages.ToList ();

			var missingDiagnostics = new List<(AttributeSyntax Attribute, string Message)> ();
			foreach (var attribute in attributes) {
				if (attribute.Name.ToString () == "LogDoesNotContain")
					ValidateLogDoesNotContainAttribute (attribute, DiagnosticMessages);

				if (!IsExpectedDiagnostic (attribute))
					continue;

				if (!TryValidateExpectedDiagnostic (attribute, unmatchedDiagnostics, out int? matchIndex, out string? missingDiagnosticMessage)) {
					missingDiagnostics.Add ((attribute, missingDiagnosticMessage));
					continue;
				}

				unmatchedDiagnostics.RemoveAt (matchIndex.Value);
			}

			var missingDiagnosticsMessage = missingDiagnostics.Any ()
				? $"Missing diagnostics:{Environment.NewLine}{string.Join (Environment.NewLine, missingDiagnostics.Select (md => md.Message))}"
				: String.Empty;

			var unmatchedDiagnosticsMessage = unmatchedDiagnostics.Any ()
				? $"Found unmatched diagnostics:{Environment.NewLine}{string.Join (Environment.NewLine, unmatchedDiagnostics)}"
				: String.Empty;

			Assert.True (!missingDiagnostics.Any (), $"{missingDiagnosticsMessage}{Environment.NewLine}{unmatchedDiagnosticsMessage}");
			Assert.True (!unmatchedDiagnostics.Any (), unmatchedDiagnosticsMessage);
		}

		private bool TryValidateExpectedWarningAttribute (AttributeSyntax attribute, List<Diagnostic> diagnostics, out int? matchIndex, out string? missingDiagnosticMessage)
		{
			missingDiagnosticMessage = null;
			matchIndex = null;
			var args = TestCaseUtils.GetAttributeArguments (attribute);
			string expectedWarningCode = TestCaseUtils.GetStringFromExpression (args["#0"]);

			if (!expectedWarningCode.StartsWith ("IL"))
				throw new InvalidOperationException ($"Expected warning code should start with \"IL\" prefix.");

			List<string> expectedMessages = args
				.Where (arg => arg.Key.StartsWith ("#") && arg.Key != "#0")
				.Select (arg => TestCaseUtils.GetStringFromExpression (arg.Value, SemanticModel))
				.ToList ();

			for (int i = 0; i < diagnostics.Count; i++) {
				if (Matches (diagnostics[i])) {
					matchIndex = i;
					return true;
				}
			}

			missingDiagnosticMessage = $"Expected to find warning containing:{string.Join (" ", expectedMessages.Select (m => "'" + m + "'"))}" +
					$", but no such message was found.{ Environment.NewLine}";
			return false;

			bool Matches (Diagnostic diagnostic)
			{
				if (diagnostic.Id != expectedWarningCode)
					return false;

				foreach (var expectedMessage in expectedMessages)
					if (!diagnostic.GetMessage ().Contains (expectedMessage))
						return false;

				return true;
			}
		}

		private bool TryValidateLogContainsAttribute (AttributeSyntax attribute, List<Diagnostic> diagnostics, out int? matchIndex, out string? missingDiagnosticMessage)
		{
			missingDiagnosticMessage = null;
			matchIndex = null;
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

			for (int i = 0; i < diagnostics.Count; i++) {
				if (diagnostics[i].GetMessage ().Contains (text)) {
					matchIndex = i;
					return true;
				}
			}

			missingDiagnosticMessage = $"Could not find text:\n{text}\nIn diagnostics:\n{(string.Join (Environment.NewLine, DiagnosticMessages))}";
			return false;
		}

		private void ValidateLogDoesNotContainAttribute (AttributeSyntax attribute, List<Diagnostic> diagnosticMessages)
		{
			var arg = Assert.Single (TestCaseUtils.GetAttributeArguments (attribute));
			var text = TestCaseUtils.GetStringFromExpression (arg.Value);
			foreach (var diagnostic in DiagnosticMessages)
				Assert.DoesNotContain (text, diagnostic.GetMessage ());
		}

		private bool TryValidateUnrecognizedReflectionAccessPatternAttribute (AttributeSyntax attribute, List<Diagnostic> diagnostics, out int? matchIndex, out string? missingDiagnosticMessage)
		{
			missingDiagnosticMessage = null;
			matchIndex = null;
			var args = TestCaseUtils.GetAttributeArguments (attribute);

			MemberDeclarationSyntax sourceMember = attribute.Ancestors ().OfType<MemberDeclarationSyntax> ().First ();
			if (SemanticModel.GetDeclaredSymbol (sourceMember) is not ISymbol memberSymbol)
				return false;

			string sourceMemberName = memberSymbol!.GetDisplayName ();
			string expectedReflectionMemberMethodType = TestCaseUtils.GetStringFromExpression ((TypeOfExpressionSyntax) args["#0"], SemanticModel, ISymbolExtensions.ILLinkTypeDisplayFormat);
			string expectedReflectionMemberMethodName = TestCaseUtils.GetStringFromExpression (args["#1"], SemanticModel);

			var reflectionMethodParameters = new List<string> ();
			if (args.TryGetValue ("#2", out var reflectionMethodParametersExpr) || args.TryGetValue ("reflectionMethodParameters", out reflectionMethodParametersExpr)) {
				if (reflectionMethodParametersExpr is ArrayCreationExpressionSyntax arrayReflectionMethodParametersExpr) {
					foreach (var rmp in arrayReflectionMethodParametersExpr.Initializer!.Expressions) {
						var parameterStr = rmp.Kind () == SyntaxKind.TypeOfExpression
							? TestCaseUtils.GetStringFromExpression ((TypeOfExpressionSyntax) rmp, SemanticModel, ISymbolExtensions.ILLinkMemberDisplayFormat)
							: TestCaseUtils.GetStringFromExpression (rmp, SemanticModel);
						reflectionMethodParameters.Add (parameterStr);
					}
				}
			}

			var expectedStringsInMessage = new List<string> ();
			if (args.TryGetValue ("#3", out var messageExpr) || args.TryGetValue ("message", out messageExpr)) {
				if (messageExpr is ArrayCreationExpressionSyntax arrayMessageExpr) {
					foreach (var m in arrayMessageExpr.Initializer!.Expressions)
						expectedStringsInMessage.Add (TestCaseUtils.GetStringFromExpression (m, SemanticModel));
				}
			}

			string expectedWarningCode = string.Empty;
			if (args.TryGetValue ("#4", out var messageCodeExpr) || args.TryGetValue ("messageCode", out messageCodeExpr)) {
				expectedWarningCode = TestCaseUtils.GetStringFromExpression (messageCodeExpr);
				Assert.True (expectedWarningCode.StartsWith ("IL"),
					$"The warning code specified in {messageCodeExpr.ToString ()} must start with the 'IL' prefix. Specified value: '{expectedWarningCode}'");
			}

			// Don't validate the return type becasue this is not included in the diagnostic messages.

			var sb = new StringBuilder ();

			// Format the member signature the same way Roslyn would since this is what will be included in the warning message.
			sb.Append (expectedReflectionMemberMethodType).Append (".").Append (expectedReflectionMemberMethodName);
			if (!expectedReflectionMemberMethodName.EndsWith (".get") &&
				!expectedReflectionMemberMethodName.EndsWith (".set") &&
				reflectionMethodParameters is not null)
				sb.Append ("(").Append (string.Join (", ", reflectionMethodParameters)).Append (")");

			var reflectionAccessPattern = sb.ToString ();

			for (int i = 0; i < diagnostics.Count; i++) {
				if (Matches (diagnostics[i])) {
					matchIndex = i;
					return true;
				}
			}

			missingDiagnosticMessage = $"Expected to find unrecognized reflection access pattern '{(expectedWarningCode == string.Empty ? "" : expectedWarningCode + " ")}" +
					$"{sourceMemberName}: Usage of {reflectionAccessPattern} unrecognized.";
			return false;

			bool Matches (Diagnostic diagnostic)
			{
				if (!string.IsNullOrEmpty (expectedWarningCode) && diagnostic.Id != expectedWarningCode)
					return false;

				// Don't check whether the message contains the source member name. Roslyn's diagnostics don't include the source
				// member as part of the message.

				foreach (var expectedString in expectedStringsInMessage)
					if (!diagnostic.GetMessage ().Contains (expectedString))
						return false;

				return diagnostic.GetMessage ().Contains (reflectionAccessPattern);
			}
		}
	}
}
