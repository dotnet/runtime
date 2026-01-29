// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RequiresUnsafeCodeFixProvider)), Shared]
    public sealed class RequiresUnsafeCodeFixProvider : BaseAttributeCodeFixProvider
    {
        private const string WrapInUnsafeBlockTitle = "Wrap in unsafe block";

        public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnsafe));

        public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select(dd => dd.Id).ToImmutableArray();

        private protected override LocalizableString CodeFixTitle => new LocalizableResourceString(nameof(Resources.RequiresUnsafeCodeFixTitle), Resources.ResourceManager, typeof(Resources));

        private protected override string FullyQualifiedAttributeName => RequiresUnsafeAnalyzer.FullyQualifiedRequiresUnsafeAttribute;

        private protected override AttributeableParentTargets AttributableParentTargets => AttributeableParentTargets.MethodOrConstructor;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Register the base code fix (add RequiresUnsafe attribute)
            await BaseRegisterCodeFixesAsync(context).ConfigureAwait(false);

            // Register the "wrap in unsafe block" code fix
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            if (await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            SyntaxNode targetNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            // Find the statement containing the unsafe call
            var containingStatement = targetNode.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
            if (containingStatement is null || containingStatement is BlockSyntax)
                return;

            context.RegisterCodeFix(CodeAction.Create(
                title: WrapInUnsafeBlockTitle,
                createChangedDocument: ct => WrapInUnsafeBlockAsync(document, containingStatement, ct),
                equivalenceKey: WrapInUnsafeBlockTitle), diagnostic);
        }

        private static async Task<Document> WrapInUnsafeBlockAsync(
            Document document,
            StatementSyntax statement,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Create the TODO comment
            var todoComment = SyntaxFactory.Comment("// TODO(unsafe): Baselining unsafe usage\n");

            // Create the unsafe block wrapping the statement
            var unsafeBlock = SyntaxFactory.UnsafeStatement(
                SyntaxFactory.Block(statement.WithoutTrivia()))
                .WithLeadingTrivia(statement.GetLeadingTrivia().Add(todoComment))
                .WithTrailingTrivia(statement.GetTrailingTrivia());

            editor.ReplaceNode(statement, unsafeBlock);

            return editor.GetChangedDocument();
        }

        protected override SyntaxNode[] GetAttributeArguments(ISymbol? attributableSymbol, ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic) =>
            RequiresHelpers.GetAttributeArgumentsForRequires(targetSymbol, syntaxGenerator, HasPublicAccessibility(attributableSymbol));
    }
}
#endif
