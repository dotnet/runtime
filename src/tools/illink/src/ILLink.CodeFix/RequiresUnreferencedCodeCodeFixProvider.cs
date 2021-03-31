// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (RequiresUnreferencedCodeCodeFixProvider)), Shared]
	public class RequiresUnreferencedCodeCodeFixProvider : CodeFixProvider
	{
		private const string s_title = "Add RequiresUnreferencedCode attribute to parent method";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create (RequiresUnreferencedCodeAnalyzer.DiagnosticId);

		public sealed override FixAllProvider GetFixAllProvider ()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false);

			var diagnostic = context.Diagnostics.First ();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the containing method
			SyntaxNode targetNode = root!.FindNode (diagnosticSpan);
			CSharpSyntaxNode? declarationSyntax = null;
			for (SyntaxNode? current = targetNode.Parent;
				 current is not null;
				 current = current.Parent) {
				if (current is LambdaExpressionSyntax) {
					return;
				} else if (current.IsKind (SyntaxKind.LocalFunctionStatement)
					  || current is BaseMethodDeclarationSyntax) {
					declarationSyntax = (CSharpSyntaxNode) current;
					break;
				}
			}

			if (declarationSyntax is not null) {
				var semanticModel = await context.Document
					.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false);
				var symbol = semanticModel!.Compilation.GetTypeByMetadataName (
					RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute);

				// Register a code action that will invoke the fix.
				context.RegisterCodeFix (
					CodeAction.Create (
						title: s_title,
						createChangedDocument: c => AddRequiresUnreferencedCode (
							context.Document, root, targetNode, declarationSyntax, symbol!, c),
						equivalenceKey: s_title),
					diagnostic);

			}
		}

		private static async Task<Document> AddRequiresUnreferencedCode (
			Document document,
			SyntaxNode root,
			SyntaxNode targetNode,
			CSharpSyntaxNode containingDecl,
			ITypeSymbol requiresUnreferencedCodeSymbol,
			CancellationToken cancellationToken)
		{
			var editor = new SyntaxEditor (root, document.Project.Solution.Workspace);
			var generator = editor.Generator;

			var semanticModel = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			if (semanticModel is null) {
				return document;
			}
			var containingSymbol = (IMethodSymbol?) semanticModel.GetDeclaredSymbol (containingDecl);
			var name = semanticModel.GetSymbolInfo (targetNode).Symbol?.Name;
			SyntaxNode[] attrArgs;
			if (string.IsNullOrEmpty (name) || HasPublicAccessibility (containingSymbol)) {
				attrArgs = Array.Empty<SyntaxNode> ();
			} else {
				attrArgs = new[] { generator.LiteralExpression ($"Calls {name}") };
			}

			var newAttribute = generator
				.Attribute (generator.TypeExpression (requiresUnreferencedCodeSymbol), attrArgs)
				.WithAdditionalAnnotations (
					Simplifier.Annotation,
					Simplifier.AddImportsAnnotation);

			editor.AddAttribute (containingDecl, newAttribute);

			return document.WithSyntaxRoot (editor.GetChangedRoot ());
		}

		private static bool HasPublicAccessibility (IMethodSymbol? m)
		{
			if (m is not { DeclaredAccessibility: Accessibility.Public or Accessibility.Protected }) {
				return false;
			}
			for (var t = m.ContainingType; t is not null; t = t.ContainingType) {
				if (t.DeclaredAccessibility != Accessibility.Public) {
					return false;
				}
			}
			return true;
		}
	}
}
