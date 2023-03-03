// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (UnconditionalSuppressMessageCodeFixProvider)), Shared]
	public class UnconditionalSuppressMessageCodeFixProvider : BaseAttributeCodeFixProvider
	{
		const string Justification = nameof (Justification);
		const string UnconditionalSuppressMessageAttribute = nameof (UnconditionalSuppressMessageAttribute);
		public const string FullyQualifiedUnconditionalSuppressMessageAttribute = "System.Diagnostics.CodeAnalysis." + UnconditionalSuppressMessageAttribute;

		public sealed override ImmutableArray<string> FixableDiagnosticIds
			=> (new DiagnosticId[] {
				DiagnosticId.RequiresUnreferencedCode,
				DiagnosticId.AvoidAssemblyLocationInSingleFile,
				DiagnosticId.AvoidAssemblyGetFilesInSingleFile,
				DiagnosticId.RequiresAssemblyFiles,
				DiagnosticId.RequiresDynamicCode }).Select (d => d.AsString ()).ToImmutableArray ();

		private protected override LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.UconditionalSuppressMessageCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected override string FullyQualifiedAttributeName => FullyQualifiedUnconditionalSuppressMessageAttribute;

		private protected override AttributeableParentTargets AttributableParentTargets => AttributeableParentTargets.All;

		public sealed override Task RegisterCodeFixesAsync (CodeFixContext context) => BaseRegisterCodeFixesAsync (context);

		protected override SyntaxNode[] GetAttributeArguments (ISymbol? attributableSymbol, ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic)
		{
			// Category of the attribute
			var ruleCategory = syntaxGenerator.AttributeArgument (
				syntaxGenerator.LiteralExpression (diagnostic.Descriptor.Category));

			// Identifier of the analysis rule the attribute applies to
			var ruleTitle = diagnostic.Descriptor.Title.ToString (CultureInfo.CurrentUICulture);
			var ruleId = syntaxGenerator.AttributeArgument (
				syntaxGenerator.LiteralExpression (
					string.IsNullOrWhiteSpace (ruleTitle) ? diagnostic.Id : $"{diagnostic.Id}:{ruleTitle}"));

			// The user should provide a justification for the suppression
			var suppressionJustification = syntaxGenerator.AttributeArgument (Justification,
				syntaxGenerator.LiteralExpression ("<Pending>"));

			// [UnconditionalSuppressWarning (category, id, Justification = "<Pending>")]
			return new[] { ruleCategory, ruleId, suppressionJustification };
		}
	}
}
