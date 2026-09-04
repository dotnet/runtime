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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Fixes <c>CS9377</c> and unsafe-specific <c>CS0106</c> diagnostics by removing the invalid <c>unsafe</c> modifier.
    /// The shared <c>CS0106</c> ID is filtered so modifiers unrelated to unsafe are left to their own fixes.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveInvalidUnsafeCodeFixProvider)), Shared]
    public sealed class RemoveInvalidUnsafeCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public const string UnsafeModifierHasNoEffectDiagnosticId = "CS9377";
        public const string InvalidModifierDiagnosticId = "CS0106";

        private static LocalizableString CodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.RemoveInvalidUnsafeCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [UnsafeModifierHasNoEffectDiagnosticId, InvalidModifierDiagnosticId];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            SyntaxNode targetNode = root.FindNode(
                diagnostic.Location.SourceSpan,
                getInnermostNodeForTie: true);
            if (UnsafeModifierCodeFixHelpers.FindDeclaration(targetNode) is not { } declaration)
                return;

            // CS0106 is shared by every invalid modifier. Restrict this fixer to declaration shapes where
            // Roslyn specifically reports unsafe as invalid, rather than inspecting localized message text.
            if (diagnostic.Id == InvalidModifierDiagnosticId && !IsInvalidUnsafeDeclaration(declaration))
                return;

            string title = CodeFixTitle.ToString();
            if (UnsafeMigrationSyntaxHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title,
                        cancellationToken => UnsafeModifierCodeFixHelpers.RemoveUnsafeModifierAsync(
                            context.Document,
                            declaration,
                            cancellationToken),
                        title),
                    diagnostic);
                return;
            }

            // A partial type is reported once for the merged symbol, at a declaration that need not be the one
            // carrying the modifier, so the other parts have to be visited to find it.
            if (declaration is not BaseTypeDeclarationSyntax
                || await GetPartsWithUnsafeModifierAsync(context.Document, declaration, context.CancellationToken).ConfigureAwait(false) is not { Length: > 0 } parts)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    cancellationToken => RemoveUnsafeModifierFromPartsAsync(
                        context.Document.Project.Solution,
                        parts,
                        cancellationToken),
                    title),
                diagnostic);
        }

        /// <summary>
        /// Returns the document IDs containing partial type declarations that carry the <c>unsafe</c> modifier in source.
        /// </summary>
        private static async Task<ImmutableArray<DocumentId>> GetPartsWithUnsafeModifierAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel?.GetDeclaredSymbol(declaration, cancellationToken) is not { } symbol)
                return [];

            Solution solution = document.Project.Solution;
            var parts = ImmutableArray.CreateBuilder<DocumentId>();
            foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode part = reference.GetSyntax(cancellationToken);
                if (!UnsafeMigrationSyntaxHelpers.HasModifier(part, SyntaxKind.UnsafeKeyword))
                    continue;

                // A modifier in a generated part cannot be edited, and for the interop generators it is also
                // what keeps the generated stub legal under the legacy rules, so it has to stay.
                if (solution.GetDocumentId(part.SyntaxTree) is { } partId && !parts.Contains(partId))
                    parts.Add(partId);
            }

            return parts.ToImmutable();
        }

        /// <summary>
        /// Removes the <c>unsafe</c> modifier from every listed partial declaration.
        /// </summary>
        private static async Task<Solution> RemoveUnsafeModifierFromPartsAsync(
            Solution solution,
            ImmutableArray<DocumentId> parts,
            CancellationToken cancellationToken)
        {
            foreach (DocumentId partId in parts)
            {
                if (solution.GetDocument(partId) is not { } part
                    || await part.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } partRoot)
                {
                    continue;
                }

                foreach (BaseTypeDeclarationSyntax type in partRoot.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    if (!UnsafeMigrationSyntaxHelpers.HasModifier(type, SyntaxKind.UnsafeKeyword))
                        continue;

                    Document changed = await UnsafeModifierCodeFixHelpers.RemoveUnsafeModifierAsync(
                        part,
                        type,
                        cancellationToken).ConfigureAwait(false);
                    solution = changed.Project.Solution;
                    break;
                }
            }

            return solution;
        }

        /// <summary>
        /// Identifies the well-formed declarations for which Roslyn reports unsafe-specific <c>CS0106</c>.
        /// </summary>
        private static bool IsInvalidUnsafeDeclaration(SyntaxNode declaration) =>
            declaration switch
            {
                EnumDeclarationSyntax => true,
                FieldDeclarationSyntax { Modifiers: var modifiers } => modifiers.Any(SyntaxKind.ConstKeyword),
                _ => false,
            };
    }
}
#endif
