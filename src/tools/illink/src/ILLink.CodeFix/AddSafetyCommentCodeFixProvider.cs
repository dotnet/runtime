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
    /// <summary>
    /// Fixes analyzer diagnostic <c>IL5009</c> by inserting a <c>// SAFETY: TODO</c> stub above an undocumented
    /// <c>unsafe</c> region.
    /// </summary>
    /// <remarks>
    /// The stub only marks the region for review. It deliberately does not attempt to describe the reasoning,
    /// which is what the developer has to supply.
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddSafetyCommentCodeFixProvider)), Shared]
    public sealed class AddSafetyCommentCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        private const string SafetyComment = "// SAFETY: TODO";

        private static LocalizableString CodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.AddSafetyCommentCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [DiagnosticId.UnsafeBlockMissingSafetyComment.AsString()];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            SyntaxToken unsafeKeyword = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (!unsafeKeyword.IsKind(SyntaxKind.UnsafeKeyword)
                || GetCommentTarget(unsafeKeyword) is not { } target)
            {
                return;
            }

            string title = CodeFixTitle.ToString();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    cancellationToken => AddSafetyCommentAsync(context.Document, target, cancellationToken),
                    title),
                diagnostic);
        }

        private static async Task<Document> AddSafetyCommentAsync(
            Document document,
            SyntaxNode target,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // The comment goes on its own line directly above the target, keeping any existing leading trivia
            // such as XML documentation or other comments in place.
            SyntaxTriviaList leadingTrivia = target.GetLeadingTrivia();
            int insertionIndex = GetInsertionIndex(leadingTrivia);
            SyntaxTriviaList updated = leadingTrivia.InsertRange(
                insertionIndex,
                new[] { SyntaxFactory.Comment(SafetyComment), SyntaxFactory.ElasticCarriageReturnLineFeed });

            editor.ReplaceNode(target, target.WithLeadingTrivia(updated));
            return editor.GetChangedDocument();
        }

        /// <summary>
        /// Inserts after any trivia that must stay attached to the line above, such as directives.
        /// </summary>
        private static int GetInsertionIndex(SyntaxTriviaList leadingTrivia)
        {
            int index = 0;
            for (int i = 0; i < leadingTrivia.Count; i++)
            {
                if (leadingTrivia[i].IsDirective || leadingTrivia[i].IsKind(SyntaxKind.DisabledTextTrivia))
                    index = i + 1;
            }

            return index;
        }

        /// <summary>
        /// Finds the node the comment should be attached to.
        /// </summary>
        /// <remarks>
        /// An <c>unsafe</c> block is its own statement, but an <c>unsafe</c> expression sits inside a larger
        /// statement, and a comment reads better above that statement than inline before the keyword.
        /// </remarks>
        private static SyntaxNode? GetCommentTarget(SyntaxToken unsafeKeyword)
        {
            if (unsafeKeyword.Parent is null)
                return null;

            if (unsafeKeyword.Parent.IsKind(SyntaxKind.UnsafeStatement))
                return unsafeKeyword.Parent;

            return unsafeKeyword.Parent.AncestorsAndSelf()
                .FirstOrDefault(static ancestor => ancestor is StatementSyntax or MemberDeclarationSyntax);
        }
    }
}
#endif
