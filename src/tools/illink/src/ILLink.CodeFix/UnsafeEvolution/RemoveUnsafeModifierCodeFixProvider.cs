// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix.UnsafeEvolution
{
    /// <summary>
    /// Removes the <c>unsafe</c> modifier from declarations where it is meaningless
    /// (IL5005 / CS9377) or where it is probably unnecessary (IL5006).
    /// </summary>
    /// <remarks>
    /// The fixer is intentionally conservative: it only removes the modifier itself,
    /// never the entire declaration, and the <see cref="UnsafeEvolutionAnalyzer"/> rules
    /// already exclude cases that are not safe to rewrite (extern, partial, nested under
    /// an unsafe type, etc).
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveUnsafeModifierCodeFixProvider)), Shared]
    public sealed class RemoveUnsafeModifierCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        private const string Title = "Remove 'unsafe' modifier";

        public override ImmutableArray<string> FixableDiagnosticIds =>
        [
            UnsafeEvolutionDescriptors.MeaninglessUnsafeModifierId,
            UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifierId,
            UnsafeEvolutionDescriptors.UnsafeMeaningless,
        ];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            if (await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            var declaration = FindDeclarationWithUnsafeModifier(root, diagnostic.Location.SourceSpan);
            if (declaration is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: ct => RemoveUnsafeAsync(document, declaration, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static SyntaxNode? FindDeclarationWithUnsafeModifier(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            var token = root.FindToken(span.Start);
            for (var node = token.Parent; node is not null; node = node.Parent)
            {
                if (GetModifiers(node) is { } modifiers && modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return node;
            }
            return null;
        }

        private static SyntaxTokenList? GetModifiers(SyntaxNode node) => node switch
        {
            BaseTypeDeclarationSyntax t => t.Modifiers,
            DelegateDeclarationSyntax d => d.Modifiers,
            BaseMethodDeclarationSyntax m => m.Modifiers,
            LocalFunctionStatementSyntax lf => lf.Modifiers,
            BasePropertyDeclarationSyntax p => p.Modifiers,
            BaseFieldDeclarationSyntax f => f.Modifiers,
            AccessorDeclarationSyntax a => a.Modifiers,
            _ => null,
        };

        private static async Task<Document> RemoveUnsafeAsync(Document document, SyntaxNode declaration, CancellationToken ct)
        {
            // Use SyntaxGenerator to remove the modifier - this correctly transfers leading
            // trivia (indentation, comments) from the removed token to whichever syntax now
            // occupies the leading position of the declaration.
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
            var modifiers = editor.Generator.GetModifiers(declaration);
            editor.SetModifiers(declaration, modifiers.WithIsUnsafe(false));
            return editor.GetChangedDocument();
        }
    }
}
#endif
