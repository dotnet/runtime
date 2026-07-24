// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
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
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Centralizes declaration discovery and trivia-preserving modifier edits for the unsafe-v2 code-fix providers.
    /// It is shared by unsafe migration fixes for compiler diagnostics, <c>IL5005</c>, and <c>IL5006</c>.
    /// </summary>
    internal static class UnsafeModifierCodeFixHelpers
    {
        internal const string SafetyDocumentationText = "TODO: Audit";

        private static readonly SyntaxAnnotation s_safetyDocumentationAnnotation =
            new(nameof(s_safetyDocumentationAnnotation));

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
                || UnsafeMigrationSyntaxHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword)
                || UnsafeMigrationSyntaxHelpers.HasSafeModifier(declaration))
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
        internal static async Task<Document> AddUnsafeModifierAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxTokenList modifiers = UnsafeMigrationSyntaxHelpers.GetModifiers(declaration);
            if (declaration is AccessorDeclarationSyntax accessor)
            {
                editor.ReplaceNode(accessor, AddUnsafeModifier(accessor));
            }
            else if (modifiers.Count > 0)
            {
                editor.ReplaceNode(
                    declaration,
                    WithModifiers(declaration, AddUnsafeModifier(modifiers)));
            }
            else
            {
                DeclarationModifiers declarationModifiers = editor.Generator.GetModifiers(declaration);
                editor.SetModifiers(declaration, declarationModifiers.WithIsUnsafe(true));
            }

            return editor.GetChangedDocument();
        }

        /// <summary>
        /// Replaces an explicit safe contract with unsafe, or adds unsafe when no safety modifier is present.
        /// The declaration is annotated so that <see cref="AddPendingSafetyDocumentationAsync"/> can document the
        /// new caller-unsafe contract, which also keeps <c>IL5005</c> from immediately removing the modifier again.
        /// </summary>
        internal static async Task<Document> SetUnsafeModifierAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            // Only contracts that this fix introduces are documented; a modifier the developer already wrote stays
            // subject to the IL5005 rules.
            if (UnsafeMigrationSyntaxHelpers.GetSafeModifier(declaration) != default)
            {
                (document, declaration) = await AnnotateForSafetyDocumentationAsync(
                    document,
                    declaration,
                    cancellationToken).ConfigureAwait(false);

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                SyntaxToken safeModifier = UnsafeMigrationSyntaxHelpers.GetSafeModifier(declaration);
                if (root is null || safeModifier == default)
                    return document;

                SyntaxToken unsafeModifier = SyntaxFactory.Token(SyntaxKind.UnsafeKeyword)
                    .WithTriviaFrom(safeModifier);
                return document.WithSyntaxRoot(root.ReplaceToken(safeModifier, unsafeModifier));
            }

            if (UnsafeMigrationSyntaxHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword))
                return document;

            (document, declaration) = await AnnotateForSafetyDocumentationAsync(
                document,
                declaration,
                cancellationToken).ConfigureAwait(false);
            return await AddUnsafeModifierAsync(document, declaration, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Marks a declaration so that <see cref="AddPendingSafetyDocumentationAsync"/> documents its new contract.
        /// </summary>
        internal static SyntaxNode MarkForSafetyDocumentation(SyntaxNode declaration) =>
            declaration.WithAdditionalAnnotations(s_safetyDocumentationAnnotation);

        /// <summary>
        /// Documents every declaration that <see cref="SetUnsafeModifierAsync"/> marked as caller-unsafe.
        /// </summary>
        internal static async Task<Document> AddPendingSafetyDocumentationAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
                return document;

            SyntaxNode[] documentationTargets = root.GetAnnotatedNodes(s_safetyDocumentationAnnotation)
                .Select(GetSafetyDocumentationTarget)
                .Distinct()
                .Where(static target => !UnsafeMigrationSyntaxHelpers.HasSafetyDocumentation(target))
                .ToArray();
            if (documentationTargets.Length > 0)
            {
                root = root.ReplaceNodes(
                    documentationTargets,
                    (_, target) => AddSafetyDocumentation(target, cancellationToken));
            }

            SyntaxNode[] annotated = root.GetAnnotatedNodes(s_safetyDocumentationAnnotation).ToArray();
            if (annotated.Length > 0)
            {
                root = root.ReplaceNodes(
                    annotated,
                    static (_, node) => node.WithoutAnnotations(s_safetyDocumentationAnnotation));
            }

            return document.WithSyntaxRoot(root);
        }

        private static async Task<(Document Document, SyntaxNode Declaration)> AnnotateForSafetyDocumentationAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            if (declaration.HasAnnotation(s_safetyDocumentationAnnotation))
                return (document, declaration);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
                return (document, declaration);

            var annotation = new SyntaxAnnotation();
            SyntaxNode annotatedDeclaration = declaration
                .WithAdditionalAnnotations(s_safetyDocumentationAnnotation, annotation);
            document = document.WithSyntaxRoot(root.ReplaceNode(declaration, annotatedDeclaration));

            root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return (document, root?.GetAnnotatedNodes(annotation).FirstOrDefault() ?? annotatedDeclaration);
        }

        /// <summary>
        /// Finds the declaration that owns the documentation for a caller-unsafe contract.
        /// Accessors cannot carry XML documentation, so their contract is documented on the containing member.
        /// </summary>
        private static SyntaxNode GetSafetyDocumentationTarget(SyntaxNode declaration) =>
            declaration is AccessorDeclarationSyntax
                ? declaration.Ancestors().OfType<BasePropertyDeclarationSyntax>().FirstOrDefault() ?? declaration
                : declaration;

        private static SyntaxNode AddSafetyDocumentation(SyntaxNode declaration, CancellationToken cancellationToken)
        {
            SyntaxTriviaList leadingTrivia = declaration.GetLeadingTrivia();
            string indentation = leadingTrivia.LastOrDefault(static trivia =>
                trivia.IsKind(SyntaxKind.WhitespaceTrivia)).ToFullString();
            SyntaxTriviaList documentation = SyntaxFactory.ParseLeadingTrivia(
                $"/// <{UnsafeMigrationSyntaxHelpers.SafetyDocumentationElement}>{SafetyDocumentationText}"
                    + $"</{UnsafeMigrationSyntaxHelpers.SafetyDocumentationElement}>"
                    + $"{GetEndOfLine(declaration, cancellationToken)}{indentation}");
            return declaration.WithLeadingTrivia(leadingTrivia.AddRange(documentation));
        }

        /// <summary>
        /// Reuses the line ending the file already uses so generated documentation does not mix line endings.
        /// </summary>
        private static string GetEndOfLine(SyntaxNode declaration, CancellationToken cancellationToken)
        {
            SyntaxTrivia endOfLine = declaration.GetLeadingTrivia()
                .FirstOrDefault(static trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
            if (endOfLine != default)
                return endOfLine.ToFullString();

            SourceText text = declaration.SyntaxTree.GetText(cancellationToken);
            foreach (TextLine line in text.Lines)
            {
                if (line.EndIncludingLineBreak > line.End)
                    return text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
            }

            return SyntaxFactory.CarriageReturnLineFeed.ToFullString();
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
            // Place unsafe before syntax-sensitive extern/partial modifiers while preserving other modifier order.
            int insertionIndex = modifiers.IndexOf(SyntaxKind.ExternKeyword);
            int partialIndex = modifiers.IndexOf(SyntaxKind.PartialKeyword);
            if (partialIndex >= 0 && (insertionIndex < 0 || partialIndex < insertionIndex))
                insertionIndex = partialIndex;
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

        internal static SyntaxNode WithModifiers(SyntaxNode declaration, SyntaxTokenList modifiers) =>
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

