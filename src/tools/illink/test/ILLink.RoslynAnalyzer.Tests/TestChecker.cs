// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	internal sealed class TestChecker : CSharpSyntaxWalker
	{
		private readonly CSharpSyntaxTree _tree;
		private readonly SemanticModel _semanticModel;
		private readonly IReadOnlyList<Diagnostic> _diagnostics;
		private readonly List<Diagnostic> _unmatched;
		private readonly List<(AttributeSyntax Attribute, string Message)> _missing;
		private readonly List<AttributeSyntax> _expectedNoWarnings;

		public TestChecker (
			CSharpSyntaxTree tree,
			SemanticModel semanticModel,
			ImmutableArray<Diagnostic> diagnostics)
		{
			_tree = tree;
			_semanticModel = semanticModel;
			_diagnostics = diagnostics
				// Filter down to diagnostics which originate from this tree or have no location
				.Where (d => d.Location.SourceTree == tree || d.Location.SourceTree == null).ToList ();

			// Filled in later
			_unmatched = new List<Diagnostic> ();
			_missing = new List<(AttributeSyntax Attribute, string Message)> ();
			_expectedNoWarnings = new List<AttributeSyntax> ();
		}

		public void Check (bool allowMissingWarnings)
		{
			_unmatched.Clear ();
			_unmatched.AddRange (_diagnostics);
			_missing.Clear ();
			_expectedNoWarnings.Clear ();

			Visit (_tree.GetRoot ());

			string message = "";
			if (!allowMissingWarnings && _missing.Any ()) {
				var missingLines = string.Join (
					Environment.NewLine,
					_missing.Select (md => $"({md.Attribute.Parent?.Parent?.GetLocation ().GetLineSpan ()}) {md.Message}"));
				message += $@"Expected warnings were not generated:{Environment.NewLine}{missingLines}{Environment.NewLine}";
			}
			var unexpected = _unmatched.Where (diag =>
				diag.Location.SourceTree == null ||
				_expectedNoWarnings.Any (attr => attr.Parent?.Parent?.Span.Contains (diag.Location.SourceSpan) == true));
			if (unexpected.Any ()) {
				message += $"Unexpected warnings were generated:{Environment.NewLine}{string.Join (Environment.NewLine, unexpected)}";
			}

			if (message.Length > 0) {
				Assert.Fail(message);
			}
		}

		public override void VisitCompilationUnit (CompilationUnitSyntax node)
		{
			base.VisitCompilationUnit (node);
			ValidateDiagnostics (node, node.AttributeLists);
		}

		public override void VisitClassDeclaration (ClassDeclarationSyntax node)
		{
			base.VisitClassDeclaration (node);
			CheckMember (node);
		}

		public override void VisitConstructorDeclaration (ConstructorDeclarationSyntax node)
		{
			base.VisitConstructorDeclaration (node);
			CheckMember (node);
		}

		public override void VisitInterfaceDeclaration (InterfaceDeclarationSyntax node)
		{
			base.VisitInterfaceDeclaration (node);
			CheckMember (node);
		}

		public override void VisitMethodDeclaration (MethodDeclarationSyntax node)
		{
			base.VisitMethodDeclaration (node);
			CheckMember (node);
		}

		public override void VisitPropertyDeclaration (PropertyDeclarationSyntax node)
		{
			base.VisitPropertyDeclaration (node);
			CheckMember (node);
		}

		public override void VisitEventDeclaration (EventDeclarationSyntax node)
		{
			base.VisitEventDeclaration (node);
			CheckMember (node);
		}

		public override void VisitEventFieldDeclaration (EventFieldDeclarationSyntax node)
		{
			base.VisitEventFieldDeclaration (node);
			CheckMember (node);
		}

		public override void VisitFieldDeclaration (FieldDeclarationSyntax node)
		{
			base.VisitFieldDeclaration (node);
			CheckMember (node);
		}

		private void CheckMember (MemberDeclarationSyntax node)
		{
			ValidateDiagnostics (node, node.AttributeLists);
		}

		public override void VisitLocalFunctionStatement (LocalFunctionStatementSyntax node)
		{
			base.VisitLocalFunctionStatement (node);
			ValidateDiagnostics (node, node.AttributeLists);
		}

		public override void VisitSimpleLambdaExpression (SimpleLambdaExpressionSyntax node)
		{
			base.VisitSimpleLambdaExpression (node);
			ValidateDiagnostics (node, node.AttributeLists);
		}

		public override void VisitParenthesizedLambdaExpression (ParenthesizedLambdaExpressionSyntax node)
		{
			base.VisitParenthesizedLambdaExpression (node);
			ValidateDiagnostics (node, node.AttributeLists);
		}

		public override void VisitAccessorDeclaration (AccessorDeclarationSyntax node)
		{
			base.VisitAccessorDeclaration (node);
			ValidateDiagnostics (node, node.AttributeLists);
		}

		private void ValidateDiagnostics (CSharpSyntaxNode memberSyntax, SyntaxList<AttributeListSyntax> attrLists)
		{
			var memberDiagnostics = _unmatched.Where (d => {
				// Filter down to diagnostics which originate from this member
				if (memberSyntax is ClassDeclarationSyntax classSyntax) {
					if (_semanticModel.GetDeclaredSymbol (classSyntax) is not ITypeSymbol typeSymbol)
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
			}).ToList ();

			foreach (var attrList in attrLists) {
				foreach (var attribute in attrList.Attributes) {
					switch (attribute.Name.ToString ()) {
					case "LogDoesNotContain":
						ValidateLogDoesNotContainAttribute (attribute, memberDiagnostics);
						break;
					case "ExpectedNoWarnings":
						_expectedNoWarnings.Add (attribute);
						break;
					}

					if (!IsExpectedDiagnostic (attribute))
						continue;

					if (!TryValidateExpectedDiagnostic (attribute, memberDiagnostics, out int? matchIndexResult, out string? missingDiagnosticMessage)) {
						_missing.Add ((attribute, missingDiagnosticMessage));
						continue;
					}

					int matchIndex = matchIndexResult.GetValueOrDefault ();
					var diagnostic = memberDiagnostics[matchIndex];
					memberDiagnostics.RemoveAt (matchIndex);
					Assert.True (_unmatched.Remove (diagnostic));
				}
			}
		}

		static bool IsExpectedDiagnostic (AttributeSyntax attribute)
		{
			switch (attribute.Name.ToString ()) {
			case "ExpectedWarning":
			case "LogContains":
				var args = LinkerTestBase.GetAttributeArguments (attribute);
				if (args.TryGetValue ("ProducedBy", out var producedBy)) {
					// Skip if this warning is not expected to be produced by any of the analyzers that we are currently testing.
					return GetProducedBy (producedBy).HasFlag (Tool.Analyzer);
				}

				return true;
			default:
				return false;
			}

			static Tool GetProducedBy (ExpressionSyntax expression)
			{
				var producedBy = (Tool) 0x0;
				switch (expression) {
				case BinaryExpressionSyntax binaryExpressionSyntax:
					if (!Enum.TryParse<Tool> ((binaryExpressionSyntax.Left as MemberAccessExpressionSyntax)!.Name.Identifier.ValueText, out var besProducedBy))
						throw new ArgumentException ("Expression must be a ProducedBy value", nameof (expression));
					producedBy |= besProducedBy;
					producedBy |= GetProducedBy (binaryExpressionSyntax.Right);
					break;

				case MemberAccessExpressionSyntax memberAccessExpressionSyntax:
					if (!Enum.TryParse<Tool> (memberAccessExpressionSyntax.Name.Identifier.ValueText, out var maeProducedBy))
						throw new ArgumentException ("Expression must be a ProducedBy value", nameof (expression));
					producedBy |= maeProducedBy;
					break;

				default:
					break;
				}

				return producedBy;
			}
		}

		bool TryValidateExpectedDiagnostic (AttributeSyntax attribute, List<Diagnostic> diagnostics, [NotNullWhen (true)] out int? matchIndex, [NotNullWhen (false)] out string? missingDiagnosticMessage)
		{
			switch (attribute.Name.ToString ()) {
			case "ExpectedWarning":
				return TryValidateExpectedWarningAttribute (attribute!, diagnostics, out matchIndex, out missingDiagnosticMessage);
			case "LogContains":
				return TryValidateLogContainsAttribute (attribute!, diagnostics, out matchIndex, out missingDiagnosticMessage);
			default:
				throw new InvalidOperationException ($"Unsupported attribute type {attribute.Name}");
			}
		}

		private bool TryValidateExpectedWarningAttribute (AttributeSyntax attribute, List<Diagnostic> diagnostics, out int? matchIndex, out string? missingDiagnosticMessage)
		{
			missingDiagnosticMessage = null;
			matchIndex = null;
			var args = LinkerTestBase.GetAttributeArguments (attribute);
			string expectedWarningCode = LinkerTestBase.GetStringFromExpression (args["#0"], _semanticModel);

			if (!expectedWarningCode.StartsWith ("IL"))
				throw new InvalidOperationException ($"Expected warning code should start with \"IL\" prefix.");

			List<string> expectedMessages = args
				.Where (arg => arg.Key.StartsWith ("#") && arg.Key != "#0")
				.Select (arg => LinkerTestBase.GetStringFromExpression (arg.Value, _semanticModel))
				.ToList ();

			for (int i = 0; i < diagnostics.Count; i++) {
				if (Matches (diagnostics[i])) {
					matchIndex = i;
					return true;
				}
			}

			missingDiagnosticMessage = $"Warning '{expectedWarningCode}'. Expected to find warning containing:{string.Join (" ", expectedMessages.Select (m => "'" + m + "'"))}" +
					$", but no such message was found.{Environment.NewLine}";
			return false;

			bool Matches (Diagnostic diagnostic)
			{
				if (!attribute.Parent?.Parent?.Span.Contains (diagnostic.Location.SourceSpan) == true)
					return false;

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
			if (!LogContains (attribute, diagnostics, out matchIndex, out string text)) {
				missingDiagnosticMessage = $"Could not find text:\n{text}\nIn diagnostics:\n{string.Join (Environment.NewLine, _diagnostics)}";
				return false;
			} else {
				missingDiagnosticMessage = null;
				return true;
			}
		}

		private void ValidateLogDoesNotContainAttribute (AttributeSyntax attribute, IReadOnlyList<Diagnostic> diagnosticMessages)
		{
			var args = LinkerTestBase.GetAttributeArguments (attribute);
			var arg = args["#0"];
			Assert.False (args.ContainsKey ("#1"));
			_ = LinkerTestBase.GetStringFromExpression (arg, _semanticModel);
			if (LogContains (attribute, diagnosticMessages, out var matchIndex, out var findText)) {
				Assert.Fail($"LogDoesNotContain failure: Text\n\"{findText}\"\nfound in diagnostic:\n {diagnosticMessages[(int) matchIndex]}");
			}
		}

		private bool LogContains (AttributeSyntax attribute, IReadOnlyList<Diagnostic> diagnostics, [NotNullWhen (true)] out int? matchIndex, out string findText)
		{

			var args = LinkerTestBase.GetAttributeArguments (attribute);
			findText = LinkerTestBase.GetStringFromExpression (args["#0"], _semanticModel);

			// If the text starts with `warning IL...` then it probably follows the pattern
			//	'warning <diagId>: <location>:'
			// We don't want to repeat the location in the error message for the analyzer, so
			// it's better to just trim here. We've already filtered by diagnostic location so
			// the text location shouldn't matter
			if (findText.StartsWith ("warning IL")) {
				var firstColon = findText.IndexOf (": ");
				if (firstColon > 0) {
					var secondColon = findText.IndexOf (": ", firstColon + 1);
					if (secondColon > 0) {
						findText = findText.Substring (secondColon + 2);
					}
				}
			}

			bool isRegex = args.TryGetValue ("regexMatch", out var regexMatchExpr)
					&& regexMatchExpr.GetLastToken ().Value is bool regexMatch
					&& regexMatch;
			if (isRegex) {
				var regex = new Regex (findText);
				for (int i = 0; i < diagnostics.Count; i++) {
					if (regex.IsMatch (diagnostics[i].GetMessage ())) {
						matchIndex = i;
						return true;
					}
				}
			} else {
				for (int i = 0; i < diagnostics.Count; i++) {
					if (diagnostics[i].GetMessage ().Contains (findText)) {
						matchIndex = i;
						return true;
					}
				}
			}
			matchIndex = null;
			return false;
		}
	}
}
