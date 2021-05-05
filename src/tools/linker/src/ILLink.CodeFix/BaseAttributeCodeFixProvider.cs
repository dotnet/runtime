// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
	public abstract class BaseAttributeCodeFixProvider : CodeFixProvider
	{
		public sealed override FixAllProvider GetFixAllProvider ()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		internal async Task BaseRegisterCodeFixesAsync (CodeFixContext context, AttributeableParentTargets targets, string fullyQualifiedAttributeName, string s_title)
		{
			var root = await context.Document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false);

			var diagnostic = context.Diagnostics.First ();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			SyntaxNode targetNode = root!.FindNode (diagnosticSpan);
			CSharpSyntaxNode? declarationSyntax = FindAttributableParent (targetNode, targets);
			if (declarationSyntax is not null) {
				var semanticModel = await context.Document.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false);
				var symbol = semanticModel!.Compilation.GetTypeByMetadataName (fullyQualifiedAttributeName);
				var document = context.Document;
				var editor = new SyntaxEditor (root, document.Project.Solution.Workspace);
				var generator = editor.Generator;
				var attrArgs = GetAttributeArguments (semanticModel, targetNode, declarationSyntax, generator, diagnostic);

				// Register a code action that will invoke the fix.
				context.RegisterCodeFix (
					CodeAction.Create (
						title: s_title,
						createChangedDocument: c => AddAttribute (
							document, editor, generator, declarationSyntax, attrArgs, symbol!, c),
						equivalenceKey: s_title),
					diagnostic);
			}
		}

		private static async Task<Document> AddAttribute (
			Document document,
			SyntaxEditor editor,
			SyntaxGenerator generator,
			CSharpSyntaxNode containingDecl,
			SyntaxNode[] attrArgs,
			ITypeSymbol AttributeSymbol,
			CancellationToken cancellationToken)
		{
			var semanticModel = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			if (semanticModel is null) {
				return document;
			}
			var newAttribute = generator
				.Attribute (generator.TypeExpression (AttributeSymbol), attrArgs)
				.WithAdditionalAnnotations (
					Simplifier.Annotation,
					Simplifier.AddImportsAnnotation);

			editor.AddAttribute (containingDecl, newAttribute);

			return document.WithSyntaxRoot (editor.GetChangedRoot ());
		}

		[Flags]
		internal enum AttributeableParentTargets
		{
			Method = 0x0001,
			Property = 0x0002,
			Field = 0x0004,
			Event = 0x0008,
			All = Method | Property | Field | Event
		}

		private static CSharpSyntaxNode? FindAttributableParent (SyntaxNode node, AttributeableParentTargets targets)
		{
			SyntaxNode? parentNode = node.Parent;
			while (parentNode is not null) {
				switch (parentNode) {
				case LambdaExpressionSyntax:
					return null;
				case LocalFunctionStatementSyntax or BaseMethodDeclarationSyntax when targets.HasFlag (AttributeableParentTargets.Method):
				case PropertyDeclarationSyntax when targets.HasFlag (AttributeableParentTargets.Property):
				case FieldDeclarationSyntax when targets.HasFlag (AttributeableParentTargets.Field):
				case EventDeclarationSyntax when targets.HasFlag (AttributeableParentTargets.Event):
					return (CSharpSyntaxNode) parentNode;
				default:
					parentNode = parentNode.Parent;
					break;
				}
			}
			return null;
		}

		internal abstract SyntaxNode[] GetAttributeArguments (SemanticModel semanticModel, SyntaxNode targetNode, CSharpSyntaxNode declarationSyntax, SyntaxGenerator generator, Diagnostic diagnostic);

		internal static bool HasPublicAccessibility (IMethodSymbol? m)
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