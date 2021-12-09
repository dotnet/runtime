// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	internal class TestChecker : CSharpSyntaxWalker
	{
		private readonly CSharpSyntaxTree _tree;
		private readonly SemanticModel _semanticModel;
		private readonly IReadOnlyList<Diagnostic> _diagnostics;
		private readonly List<Diagnostic> _unmatched;
		private readonly List<(AttributeSyntax Attribute, string Message)> _missing;

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
		}

		public void Check ()
		{
			_unmatched.Clear ();
			_unmatched.AddRange (_diagnostics);
			_missing.Clear ();

			Visit (_tree.GetRoot ());

			string message = "";
			if (_missing.Any ()) {
				var missingLines = string.Join (
					Environment.NewLine,
					_missing.Select (md => $"({md.Attribute.GetLocation ().GetLineSpan ()}) {md.Message}"));
				message += $@"Expected warnings were not generated:{Environment.NewLine}{missingLines}{Environment.NewLine}";
			}
			if (_unmatched.Any ()) {

				message += $"Unexpected warnings were generated:{Environment.NewLine}{string.Join (Environment.NewLine, _unmatched)}";
			}

			if (message.Length > 0) {
				Assert.True (false, message);
			}
		}

		public override void VisitClassDeclaration (ClassDeclarationSyntax node)
		{
			base.VisitClassDeclaration (node);
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
					if (attribute.Name.ToString () == "LogDoesNotContain")
						ValidateLogDoesNotContainAttribute (attribute, memberDiagnostics);

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
					return GetProducedBy (producedBy).HasFlag (ProducedBy.Analyzer);
				}

				return true;
			case "UnrecognizedReflectionAccessPattern":
				return true;
			default:
				return false;
			}

			static ProducedBy GetProducedBy (ExpressionSyntax expression)
			{
				var producedBy = (ProducedBy) 0x0;
				switch (expression) {
				case BinaryExpressionSyntax binaryExpressionSyntax:
					if (!Enum.TryParse<ProducedBy> ((binaryExpressionSyntax.Left as MemberAccessExpressionSyntax)!.Name.Identifier.ValueText, out var besProducedBy))
						throw new ArgumentException ("Expression must be a ProducedBy value", nameof (expression));
					producedBy |= besProducedBy;
					producedBy |= GetProducedBy (binaryExpressionSyntax.Right);
					break;

				case MemberAccessExpressionSyntax memberAccessExpressionSyntax:
					if (!Enum.TryParse<ProducedBy> (memberAccessExpressionSyntax.Name.Identifier.ValueText, out var maeProducedBy))
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
			case "UnrecognizedReflectionAccessPattern":
				return TryValidateUnrecognizedReflectionAccessPatternAttribute (attribute!, diagnostics, out matchIndex, out missingDiagnosticMessage);
			default:
				throw new InvalidOperationException ($"Unsupported attribute type {attribute.Name}");
			}
		}

		private bool TryValidateExpectedWarningAttribute (AttributeSyntax attribute, List<Diagnostic> diagnostics, out int? matchIndex, out string? missingDiagnosticMessage)
		{
			missingDiagnosticMessage = null;
			matchIndex = null;
			var args = LinkerTestBase.GetAttributeArguments (attribute);
			string expectedWarningCode = LinkerTestBase.GetStringFromExpression (args["#0"]);

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
			var args = LinkerTestBase.GetAttributeArguments (attribute);
			var text = LinkerTestBase.GetStringFromExpression (args["#0"]);

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

			missingDiagnosticMessage = $"Could not find text:\n{text}\nIn diagnostics:\n{string.Join (Environment.NewLine, _diagnostics)}";
			return false;
		}

		private static void ValidateLogDoesNotContainAttribute (AttributeSyntax attribute, IReadOnlyList<Diagnostic> diagnosticMessages)
		{
			var arg = Assert.Single (LinkerTestBase.GetAttributeArguments (attribute));
			var text = LinkerTestBase.GetStringFromExpression (arg.Value);
			foreach (var diagnostic in diagnosticMessages)
				Assert.DoesNotContain (text, diagnostic.GetMessage ());
		}

		private bool TryValidateUnrecognizedReflectionAccessPatternAttribute (AttributeSyntax attribute, List<Diagnostic> diagnostics, out int? matchIndex, out string? missingDiagnosticMessage)
		{
			missingDiagnosticMessage = null;
			matchIndex = null;
			var args = LinkerTestBase.GetAttributeArguments (attribute);

			MemberDeclarationSyntax sourceMember = attribute.Ancestors ().OfType<MemberDeclarationSyntax> ().First ();
			if (_semanticModel.GetDeclaredSymbol (sourceMember) is not ISymbol memberSymbol)
				return false;

			string sourceMemberName = memberSymbol!.GetDisplayName ();
			string expectedReflectionMemberMethodType = LinkerTestBase.GetStringFromExpression (args["#0"], _semanticModel);
			string expectedReflectionMemberMethodName = LinkerTestBase.GetStringFromExpression (args["#1"], _semanticModel);

			var reflectionMethodParameters = new List<string> ();
			if (args.TryGetValue ("#2", out var reflectionMethodParametersExpr) || args.TryGetValue ("reflectionMethodParameters", out reflectionMethodParametersExpr)) {
				if (reflectionMethodParametersExpr is ArrayCreationExpressionSyntax arrayReflectionMethodParametersExpr) {
					foreach (var rmp in arrayReflectionMethodParametersExpr.Initializer!.Expressions)
						reflectionMethodParameters.Add (LinkerTestBase.GetStringFromExpression (rmp, _semanticModel));
				}
			}

			var expectedStringsInMessage = new List<string> ();
			if (args.TryGetValue ("#3", out var messageExpr) || args.TryGetValue ("message", out messageExpr)) {
				if (messageExpr is ArrayCreationExpressionSyntax arrayMessageExpr) {
					foreach (var m in arrayMessageExpr.Initializer!.Expressions)
						expectedStringsInMessage.Add (LinkerTestBase.GetStringFromExpression (m, _semanticModel));
				}
			}

			string expectedWarningCode = string.Empty;
			if (args.TryGetValue ("#4", out var messageCodeExpr) || args.TryGetValue ("messageCode", out messageCodeExpr)) {
				expectedWarningCode = LinkerTestBase.GetStringFromExpression (messageCodeExpr);
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
