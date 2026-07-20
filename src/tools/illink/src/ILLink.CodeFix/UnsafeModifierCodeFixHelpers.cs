// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
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

namespace ILLink.CodeFix
{
    /// <summary>
    /// Centralizes modifier discovery and trivia-preserving edits for the unsafe-v2 code-fix providers.
    /// It is shared by fixes for <c>CS9389</c>, <c>CS9377</c>/<c>CS0106</c>, <c>IL5005</c>, <c>IL5006</c>, and <c>CS9392</c>.
    /// </summary>
    internal static class UnsafeModifierCodeFixHelpers
    {
        /// <summary>
        /// Registers an add-unsafe action for a supported declaration that has no existing safety modifier.
        /// </summary>
        internal static async Task RegisterAddUnsafeCodeFixAsync(
            CodeFixContext context,
            LocalizableString codeFixTitle,
            Func<SyntaxNode, bool> isSupportedDeclaration)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            SyntaxNode targetNode = root.FindNode(
                context.Diagnostics[0].Location.SourceSpan,
                getInnermostNodeForTie: true);
            if (FindDeclaration(targetNode) is not { } declaration
                || !isSupportedDeclaration(declaration)
                || HasModifier(declaration, SyntaxKind.UnsafeKeyword)
                || HasSafeModifier(declaration))
            {
                return;
            }

            string title = codeFixTitle.ToString();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    cancellationToken => AddUnsafeModifierAsync(context.Document, declaration, cancellationToken),
                    title),
                context.Diagnostics[0]);
        }

        /// <summary>
        /// Finds the nearest declaration whose modifier list can contain unsafe-v2 contract markers.
        /// </summary>
        internal static SyntaxNode? FindDeclaration(SyntaxNode node) =>
            node.AncestorsAndSelf().FirstOrDefault(static ancestor =>
                ancestor is MemberDeclarationSyntax
                    or LocalFunctionStatementSyntax
                    or AccessorDeclarationSyntax);

        internal static bool HasModifier(SyntaxNode declaration, SyntaxKind modifier) =>
            GetModifiers(declaration).Any(modifier);

        /// <summary>
        /// Checks for the contextual <c>safe</c> modifier without depending on a newer SyntaxKind API.
        /// </summary>
        internal static bool HasSafeModifier(SyntaxNode declaration)
        {
            foreach (SyntaxToken modifier in GetModifiers(declaration))
            {
                if (modifier.ValueText == "safe")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Adds unsafe while preserving declaration-specific modifiers and trivia.
        /// </summary>
        internal static async Task<Document> AddUnsafeModifierAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            if (declaration is AccessorDeclarationSyntax accessor)
            {
                editor.ReplaceNode(accessor, AddUnsafeModifier(accessor));
            }
            else if (declaration is DestructorDeclarationSyntax destructor)
            {
                // SyntaxGenerator does not preserve extern on destructors in the current Workspaces dependency.
                editor.ReplaceNode(
                    destructor,
                    destructor.WithModifiers(AddUnsafeModifier(destructor.Modifiers)));
            }
            else
            {
                var modifiers = editor.Generator.GetModifiers(declaration);
                editor.SetModifiers(declaration, modifiers.WithIsUnsafe(true));
            }

            return editor.GetChangedDocument();
        }

        /// <summary>
        /// Removes unsafe without disturbing modifiers that the current SyntaxGenerator does not model.
        /// </summary>
        internal static async Task<Document> RemoveUnsafeModifierAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            if (declaration is AccessorDeclarationSyntax accessor)
            {
                editor.ReplaceNode(accessor, RemoveUnsafeModifier(accessor));
            }
            else if (declaration is DestructorDeclarationSyntax destructor
                && destructor.Modifiers.Any(SyntaxKind.ExternKeyword))
            {
                // Remove only unsafe; routing this through SyntaxGenerator would also remove extern.
                editor.ReplaceNode(
                    destructor,
                    destructor.WithModifiers(RemoveUnsafeModifier(destructor.Modifiers)));
            }
            else
            {
                var modifiers = editor.Generator.GetModifiers(declaration);
                editor.SetModifiers(declaration, modifiers.WithIsUnsafe(false));
            }

            return editor.GetChangedDocument();
        }

        private static SyntaxTokenList GetModifiers(SyntaxNode declaration) =>
            declaration switch
            {
                MemberDeclarationSyntax member => member.Modifiers,
                LocalFunctionStatementSyntax localFunction => localFunction.Modifiers,
                AccessorDeclarationSyntax accessor => accessor.Modifiers,
                _ => default,
            };

        private static AccessorDeclarationSyntax AddUnsafeModifier(AccessorDeclarationSyntax accessor)
        {
            // SyntaxGenerator does not yet model unsafe property accessors, so edit their tokens directly.
            if (accessor.Modifiers.Count > 0)
                return accessor.WithModifiers(AddUnsafeModifier(accessor.Modifiers));

            SyntaxToken unsafeModifier = SyntaxFactory.Token(SyntaxKind.UnsafeKeyword)
                .WithLeadingTrivia(accessor.Keyword.LeadingTrivia)
                .WithTrailingTrivia(SyntaxFactory.ElasticSpace);
            return accessor
                .WithModifiers([unsafeModifier])
                .WithKeyword(accessor.Keyword.WithLeadingTrivia(default(SyntaxTriviaList)));
        }

        private static AccessorDeclarationSyntax RemoveUnsafeModifier(AccessorDeclarationSyntax accessor)
        {
            // Keep declaration-leading trivia attached to the first remaining token.
            SyntaxTokenList modifiers = accessor.Modifiers;
            int unsafeIndex = GetUnsafeModifierIndex(modifiers);
            SyntaxTriviaList leadingTrivia = modifiers[unsafeIndex].LeadingTrivia;
            modifiers = modifiers.RemoveAt(unsafeIndex);

            if (unsafeIndex == 0)
            {
                if (modifiers.Count > 0)
                {
                    modifiers = modifiers.Replace(
                        modifiers[0],
                        modifiers[0].WithLeadingTrivia(leadingTrivia.AddRange(modifiers[0].LeadingTrivia)));
                }
                else
                {
                    return accessor
                        .WithModifiers(modifiers)
                        .WithKeyword(accessor.Keyword.WithLeadingTrivia(leadingTrivia.AddRange(accessor.Keyword.LeadingTrivia)));
                }
            }

            return accessor.WithModifiers(modifiers);
        }

        private static SyntaxTokenList AddUnsafeModifier(SyntaxTokenList modifiers)
        {
            // Match SyntaxGenerator's canonical order by placing unsafe before extern.
            int insertionIndex = modifiers.IndexOf(SyntaxKind.ExternKeyword);
            if (insertionIndex < 0)
                insertionIndex = modifiers.Count;

            SyntaxToken unsafeModifier = SyntaxFactory.Token(SyntaxKind.UnsafeKeyword)
                .WithTrailingTrivia(SyntaxFactory.ElasticSpace);
            if (insertionIndex == 0)
            {
                unsafeModifier = unsafeModifier.WithLeadingTrivia(modifiers[0].LeadingTrivia);
                modifiers = modifiers.Replace(
                    modifiers[0],
                    modifiers[0].WithLeadingTrivia(default(SyntaxTriviaList)));
            }

            return modifiers.Insert(insertionIndex, unsafeModifier);
        }

        private static SyntaxTokenList RemoveUnsafeModifier(SyntaxTokenList modifiers)
        {
            int unsafeIndex = GetUnsafeModifierIndex(modifiers);
            SyntaxTriviaList leadingTrivia = modifiers[unsafeIndex].LeadingTrivia;
            modifiers = modifiers.RemoveAt(unsafeIndex);

            // If unsafe owned the declaration's leading trivia, move it to the next modifier.
            if (unsafeIndex == 0 && modifiers.Count > 0)
            {
                modifiers = modifiers.Replace(
                    modifiers[0],
                    modifiers[0].WithLeadingTrivia(leadingTrivia.AddRange(modifiers[0].LeadingTrivia)));
            }

            return modifiers;
        }

        private static int GetUnsafeModifierIndex(SyntaxTokenList modifiers) =>
            modifiers.IndexOf(SyntaxKind.UnsafeKeyword);
    }
}
#endif
