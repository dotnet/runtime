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
    /// Fixes compiler diagnostics <c>CS0764</c> and <c>CS9390</c> by copying the safety modifier from the partial
    /// declaration that has it onto the one that does not.
    /// </summary>
    /// <remarks>
    /// The modifier is always propagated rather than removed. For <c>unsafe</c> that preserves the caller
    /// obligation, and for <c>safe</c> removing it would reintroduce <c>CS9389</c> on an <c>extern</c> member.
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MatchPartialSafetyModifierCodeFixProvider)), Shared]
    public sealed class MatchPartialSafetyModifierCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public const string PartialMemberUnsafeDifferenceDiagnosticId = "CS0764";
        public const string PartialMemberSafeDifferenceDiagnosticId = "CS9390";

        private static LocalizableString CodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.MatchPartialSafetyModifierCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [PartialMemberUnsafeDifferenceDiagnosticId, PartialMemberSafeDifferenceDiagnosticId];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            SyntaxKind modifier = diagnostic.Id == PartialMemberUnsafeDifferenceDiagnosticId
                ? SyntaxKind.UnsafeKeyword
                : UnsafeMigrationSyntaxHelpers.SafeKeywordKind;
            if (modifier == SyntaxKind.None)
                return;

            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root
                || await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false) is not { } semanticModel)
            {
                return;
            }

            SyntaxNode targetNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (FindPartialDeclaration(targetNode) is not { } declaration
                || semanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not { } symbol
                || GetOtherPart(symbol) is not { } otherPart)
            {
                return;
            }

            // Determine which declaration is missing the modifier. The diagnostic is reported on the
            // implementing part regardless of which part carries it.
            bool declarationHasModifier = HasModifier(declaration, modifier);
            (Document Document, SyntaxNode Declaration)? target = declarationHasModifier
                ? await GetDeclarationToEditAsync(context.Document.Project.Solution, otherPart, modifier, context.CancellationToken).ConfigureAwait(false)
                : (context.Document, declaration);

            // When the parts disagree in opposite directions the compiler reports both CS0764 and CS9390, and
            // adding the missing modifier would put 'safe' and 'unsafe' on the same declaration, which is an
            // error. Resolving that requires deciding which contract is correct, so no fix is offered.
            if (target is not { } edit || HasModifier(edit.Declaration, GetOppositeModifier(modifier)))
                return;

            string title = CodeFixTitle.ToString();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    async cancellationToken =>
                    {
                        Document updated = await UnsafeModifierCodeFixHelpers
                            .AddModifierAsync(edit.Document, edit.Declaration, modifier, cancellationToken)
                            .ConfigureAwait(false);
                        return updated.Project.Solution;
                    },
                    title),
                diagnostic);
        }

        private static async Task<(Document, SyntaxNode)?> GetDeclarationToEditAsync(
            Solution solution,
            ISymbol otherPart,
            SyntaxKind modifier,
            CancellationToken cancellationToken)
        {
            foreach (SyntaxReference reference in otherPart.DeclaringSyntaxReferences)
            {
                // A part emitted by a source generator has no editable document.
                if (solution.GetDocument(reference.SyntaxTree) is not { } document)
                    continue;

                SyntaxNode declaration = reference.GetSyntax(cancellationToken);

                // A field-like event's reference points at the variable declarator, but the modifier lives on the
                // declaration that contains it.
                if (declaration is VariableDeclaratorSyntax variable && variable.Parent?.Parent is BaseFieldDeclarationSyntax field)
                    declaration = field;

                if (HasModifier(declaration, modifier))
                    continue;

                // Re-resolve against the document's current tree so the edit applies to a live node.
                if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
                    continue;

                SyntaxNode candidate = root.FindNode(declaration.Span, getInnermostNodeForTie: true);
                if (candidate.AncestorsAndSelf().FirstOrDefault(node => node.IsKind(declaration.Kind())) is { } current)
                    return (document, current);
            }

            return null;
        }

        private static bool HasModifier(SyntaxNode declaration, SyntaxKind modifier) =>
            modifier == SyntaxKind.UnsafeKeyword
                ? UnsafeMigrationSyntaxHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword)
                : UnsafeMigrationSyntaxHelpers.HasSafeModifier(declaration);

        private static SyntaxKind GetOppositeModifier(SyntaxKind modifier) =>
            modifier == SyntaxKind.UnsafeKeyword
                ? UnsafeMigrationSyntaxHelpers.SafeKeywordKind
                : SyntaxKind.UnsafeKeyword;

        private static ISymbol? GetOtherPart(ISymbol symbol) =>
            symbol switch
            {
                IMethodSymbol method => method.PartialDefinitionPart ?? method.PartialImplementationPart,
                IPropertySymbol property => property.PartialDefinitionPart ?? property.PartialImplementationPart,
                IEventSymbol @event => @event.PartialDefinitionPart ?? @event.PartialImplementationPart,
                _ => null,
            };

        private static SyntaxNode? FindPartialDeclaration(SyntaxNode node) =>
            // BaseMethodDeclarationSyntax covers partial methods and constructors, BasePropertyDeclarationSyntax
            // covers partial properties, indexers and events, and a field-like event is its own declaration kind.
            node.AncestorsAndSelf().FirstOrDefault(static ancestor => ancestor is BaseMethodDeclarationSyntax
                or BasePropertyDeclarationSyntax
                or EventFieldDeclarationSyntax);
    }
}
#endif
