// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (UnconditionalSuppressMessageCodeFixProvider)), Shared]
	public class UnconditionalSuppressMessageCodeFixProvider : BaseAttributeCodeFixProvider
	{
		const string UnconditionalSuppressMessageAttribute = nameof (UnconditionalSuppressMessageAttribute);
		public const string FullyQualifiedUnconditionalSuppressMessageAttribute = "System.Diagnostics.CodeAnalysis." + UnconditionalSuppressMessageAttribute;

		public sealed override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create (RequiresUnreferencedCodeAnalyzer.IL2026, RequiresAssemblyFilesAnalyzer.IL3000, RequiresAssemblyFilesAnalyzer.IL3001, RequiresAssemblyFilesAnalyzer.IL3002);

		private protected override LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.UconditionalSuppressMessageCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected override string FullyQualifiedAttributeName => FullyQualifiedUnconditionalSuppressMessageAttribute;

		private protected override AttributeableParentTargets AttributableParentTargets => AttributeableParentTargets.All;

		public sealed override Task RegisterCodeFixesAsync (CodeFixContext context) => BaseRegisterCodeFixesAsync (context);

		protected override SyntaxNode[] GetAttributeArguments (SemanticModel semanticModel, SyntaxNode targetNode, CSharpSyntaxNode containingDecl, SyntaxGenerator generator, Diagnostic diagnostic)
		{
			// UnconditionalSuppressMessage("Rule Category", "Rule Id", Justification = "<Pending>")
			var category = generator.LiteralExpression (diagnostic.Descriptor.Category);
			var categoryArgument = generator.AttributeArgument (category);

			var title = diagnostic.Descriptor.Title.ToString (CultureInfo.CurrentUICulture);
			var ruleIdText = string.IsNullOrWhiteSpace (title) ? diagnostic.Id : string.Format ("{0}:{1}", diagnostic.Id, title);
			var ruleId = generator.LiteralExpression (ruleIdText);
			var ruleIdArgument = generator.AttributeArgument (ruleId);

			var justificationExpr = generator.LiteralExpression ("<Pending>");
			var justificationArgument = generator.AttributeArgument ("Justification", justificationExpr);

			return new[] { categoryArgument, ruleIdArgument, justificationArgument };
		}

	}
}
