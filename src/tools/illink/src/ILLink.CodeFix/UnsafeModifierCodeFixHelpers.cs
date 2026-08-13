// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Centralizes declaration discovery and trivia-preserving modifier edits for the unsafe-v2 code-fix providers.
    /// It is shared by fixes for <c>CS9389</c>, <c>CS9377</c>/<c>CS0106</c>, <c>IL5005</c>, <c>IL5006</c>, and <c>CS9392</c>.
    /// </summary>
    internal static class UnsafeModifierCodeFixHelpers
    {
        /// <summary>
        /// Registers an add-unsafe action for a supported declaration that has no existing safety modifier.
        /// </summary>
        /// <remarks>
        /// A declaration that documents why it is safe gets <c>safe</c> instead, so an audited member does not
        /// become caller-unsafe and force its callers into <c>unsafe</c> contexts.
        /// </remarks>
        internal static async Task RegisterAddUnsafeCodeFixAsync(
            CodeFixContext context,
            LocalizableString codeFixTitle,
            Func<SyntaxNode, bool> isSupportedDeclaration,
            SyntaxKind insertAfterModifier = SyntaxKind.None)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            SyntaxNode targetNode = root.FindNode(
                context.Diagnostics[0].Location.SourceSpan,
                getInnermostNodeForTie: true);
            if (FindDeclaration(targetNode) is not { } declaration
                || !isSupportedDeclaration(declaration)
                || UnsafeMigrationSyntaxHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword)
                || UnsafeMigrationSyntaxHelpers.HasSafeModifier(declaration))
            {
                return;
            }

            SyntaxKind modifier = SyntaxKind.UnsafeKeyword;
            string title = codeFixTitle.ToString();
            string displayTitle = title;
            if (UnsafeMigrationSyntaxHelpers.SafeKeywordKind != SyntaxKind.None
                && UnsafeMigrationSyntaxHelpers.HasSafetyDocumentation(declaration))
            {
                modifier = UnsafeMigrationSyntaxHelpers.SafeKeywordKind;
                displayTitle = new LocalizableResourceString(
                    nameof(Resources.AddSafeToDocumentedMemberCodeFixTitle),
                    Resources.ResourceManager,
                    typeof(Resources)).ToString();
            }

            // Both flavors share an equivalence key so a single fix-all pass can apply the contract each
            // declaration asks for, rather than settling on whichever flavor it encountered first.
            context.RegisterCodeFix(
                CodeAction.Create(
                    displayTitle,
                    cancellationToken => AddModifierAsync(
                        context.Document,
                        declaration,
                        modifier,
                        cancellationToken,
                        insertAfterModifier),
                    title),
                context.Diagnostics[0]);
        }

        /// <summary>
        /// Finds the nearest declaration whose modifier list can contain unsafe-v2 contract markers.
        /// </summary>
        internal static SyntaxNode? FindDeclaration(SyntaxNode node) =>
            node.AncestorsAndSelf().FirstOrDefault(static ancestor =>
                ancestor is BaseTypeDeclarationSyntax
                    or DelegateDeclarationSyntax
                    or BaseMethodDeclarationSyntax
                    or BasePropertyDeclarationSyntax
                    or BaseFieldDeclarationSyntax
                    or LocalFunctionStatementSyntax
                    or AccessorDeclarationSyntax);

        /// <summary>
        /// Adds unsafe while preserving declaration-specific modifiers and trivia.
        /// </summary>
        internal static Task<Document> AddUnsafeModifierAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken) =>
            AddModifierAsync(document, declaration, SyntaxKind.UnsafeKeyword, cancellationToken);

        /// <summary>
        /// Adds a safety modifier while preserving declaration-specific modifiers and trivia.
        /// </summary>
        internal static async Task<Document> AddModifierAsync(
            Document document,
            SyntaxNode declaration,
            SyntaxKind modifier,
            CancellationToken cancellationToken,
            SyntaxKind insertAfterModifier = SyntaxKind.None)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(declaration, AddModifier(declaration, modifier, insertAfterModifier));
            return editor.GetChangedDocument();
        }

        /// <summary>
        /// Replaces an <c>unsafe</c> modifier with <c>safe</c>, preserving its position and trivia.
        /// </summary>
        /// <remarks>
        /// This is the narrowing edit for declarations that must keep an explicit marker, such as
        /// <c>extern</c> members, where removing <c>unsafe</c> outright would produce <c>CS9389</c>.
        /// </remarks>
        internal static async Task<Document> ReplaceUnsafeWithSafeAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxTokenList modifiers = UnsafeMigrationSyntaxHelpers.GetModifiers(declaration);
            int unsafeIndex = GetUnsafeModifierIndex(modifiers);
            if (unsafeIndex < 0 || UnsafeMigrationSyntaxHelpers.SafeKeywordKind == SyntaxKind.None)
                return document;

            SyntaxToken safeToken = SyntaxFactory.Token(UnsafeMigrationSyntaxHelpers.SafeKeywordKind)
                .WithTriviaFrom(modifiers[unsafeIndex]);
            editor.ReplaceNode(
                declaration,
                WithModifiers(declaration, modifiers.Replace(modifiers[unsafeIndex], safeToken)));
            return editor.GetChangedDocument();
        }

        /// <summary>
        /// Returns <paramref name="declaration"/> with a safety modifier added.
        /// </summary>
        internal static SyntaxNode AddModifier(
            SyntaxNode declaration,
            SyntaxKind modifier,
            SyntaxKind insertAfterModifier = SyntaxKind.None)
        {
            if (declaration is AccessorDeclarationSyntax accessor)
                return AddModifier(accessor, modifier);

            SyntaxTokenList modifiers = UnsafeMigrationSyntaxHelpers.GetModifiers(declaration);
            if (modifiers.Count > 0)
                return WithModifiers(declaration, AddModifier(modifiers, modifier, insertAfterModifier));

            // With no existing modifiers the declaration's leading trivia belongs to the token the new modifier
            // displaces, so it has to move with it. Attribute lists keep their own leading trivia.
            SyntaxToken anchor = GetModifierAnchorToken(declaration);
            SyntaxToken modifierToken = SyntaxFactory.Token(modifier)
                .WithLeadingTrivia(anchor.LeadingTrivia)
                .WithTrailingTrivia(SyntaxFactory.ElasticSpace);
            SyntaxNode withoutTrivia = declaration.ReplaceToken(
                anchor,
                anchor.WithLeadingTrivia(default(SyntaxTriviaList)));
            return WithModifiers(withoutTrivia, SyntaxFactory.TokenList(modifierToken));
        }

        private static SyntaxToken GetModifierAnchorToken(SyntaxNode declaration) =>
            GetAttributeLists(declaration) is { Count: > 0 } attributeLists
                ? attributeLists[attributeLists.Count - 1].GetLastToken().GetNextToken()
                : declaration.GetFirstToken();

        private static SyntaxList<AttributeListSyntax> GetAttributeLists(SyntaxNode declaration) =>
            declaration switch
            {
                MemberDeclarationSyntax member => member.AttributeLists,
                LocalFunctionStatementSyntax localFunction => localFunction.AttributeLists,
                AccessorDeclarationSyntax accessor => accessor.AttributeLists,
                _ => default,
            };

        /// <summary>
        /// Removes unsafe without disturbing modifiers that the current SyntaxGenerator does not model.
        /// </summary>
        internal static async Task<Document> RemoveUnsafeModifierAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxTokenList modifiers = UnsafeMigrationSyntaxHelpers.GetModifiers(declaration);
            if (declaration is AccessorDeclarationSyntax accessor)
            {
                editor.ReplaceNode(accessor, RemoveUnsafeModifier(accessor));
            }
            else if (modifiers.Count > 1)
            {
                editor.ReplaceNode(
                    declaration,
                    WithModifiers(declaration, RemoveUnsafeModifier(modifiers)));
            }
            else
            {
                DeclarationModifiers declarationModifiers = editor.Generator.GetModifiers(declaration);
                editor.SetModifiers(declaration, declarationModifiers.WithIsUnsafe(false));
            }

            return editor.GetChangedDocument();
        }

        private static AccessorDeclarationSyntax AddModifier(AccessorDeclarationSyntax accessor, SyntaxKind modifier)
        {
            // SyntaxGenerator does not yet model unsafe property accessors, so edit their tokens directly.
            if (accessor.Modifiers.Count > 0)
                return accessor.WithModifiers(AddModifier(accessor.Modifiers, modifier, SyntaxKind.None));

            SyntaxToken modifierToken = SyntaxFactory.Token(modifier)
                .WithLeadingTrivia(accessor.Keyword.LeadingTrivia)
                .WithTrailingTrivia(SyntaxFactory.ElasticSpace);
            return accessor
                .WithModifiers([modifierToken])
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

        private static SyntaxTokenList AddModifier(
            SyntaxTokenList modifiers,
            SyntaxKind modifier,
            SyntaxKind insertAfterModifier)
        {
            int insertionIndex = insertAfterModifier == SyntaxKind.None
                ? -1
                : modifiers.IndexOf(insertAfterModifier);
            if (insertionIndex >= 0)
            {
                insertionIndex++;
            }
            else
            {
                // 'extern' and 'partial' conventionally sit closest to the return type, so the safety modifier goes
                // before them. Otherwise it is appended, which matches the repo's preferred modifier order where
                // 'unsafe' follows 'virtual', 'abstract', 'sealed' and 'override'.
                insertionIndex = GetFirstModifierIndex(modifiers, SyntaxKind.ExternKeyword, SyntaxKind.PartialKeyword);
            }

            if (insertionIndex < 0)
                insertionIndex = modifiers.Count;

            SyntaxToken modifierToken = SyntaxFactory.Token(modifier)
                .WithTrailingTrivia(SyntaxFactory.ElasticSpace);
            if (insertionIndex == 0 && modifiers.Count > 0)
            {
                modifierToken = modifierToken.WithLeadingTrivia(modifiers[0].LeadingTrivia);
                modifiers = modifiers.Replace(
                    modifiers[0],
                    modifiers[0].WithLeadingTrivia(default(SyntaxTriviaList)));
            }

            return modifiers.Insert(insertionIndex, modifierToken);
        }

        private static int GetFirstModifierIndex(SyntaxTokenList modifiers, SyntaxKind first, SyntaxKind second)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].IsKind(first) || modifiers[i].IsKind(second))
                    return i;
            }

            return -1;
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

        private static SyntaxNode WithModifiers(SyntaxNode declaration, SyntaxTokenList modifiers) =>
            declaration switch
            {
                BaseTypeDeclarationSyntax type => type.WithModifiers(modifiers),
                DelegateDeclarationSyntax @delegate => @delegate.WithModifiers(modifiers),
                BaseMethodDeclarationSyntax method => method.WithModifiers(modifiers),
                BasePropertyDeclarationSyntax property => property.WithModifiers(modifiers),
                BaseFieldDeclarationSyntax field => field.WithModifiers(modifiers),
                LocalFunctionStatementSyntax localFunction => localFunction.WithModifiers(modifiers),
                AccessorDeclarationSyntax accessor => accessor.WithModifiers(modifiers),
                _ => declaration,
            };

        private static int GetUnsafeModifierIndex(SyntaxTokenList modifiers) =>
            modifiers.IndexOf(SyntaxKind.UnsafeKeyword);
    }
}
#endif
