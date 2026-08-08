// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Introduces the <c>unsafe</c> context that the updated memory safety rules require at a use site, by fixing
    /// <c>CS9360</c>, <c>CS9361</c>, <c>CS9362</c>, <c>CS9363</c> and <c>CS9376</c>.
    /// </summary>
    /// <remarks>
    /// This is the highest volume part of a migration to unsafe-v2, because every consumer of a newly caller-unsafe
    /// API breaks. Note that marking the containing member <c>unsafe</c> is <em>not</em> a fix: under the updated
    /// rules the modifier declares an obligation on the member's own callers, and no longer opens an unsafe context
    /// in its body.
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IntroduceUnsafeContextCodeFixProvider)), Shared]
    public sealed class IntroduceUnsafeContextCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        /// <summary>
        /// The number of separate unsafe regions a single body may need before the whole body is wrapped instead.
        /// </summary>
        /// <remarks>
        /// Bodies that touch pointers throughout, such as the <c>SpanHelpers</c> family, would otherwise be broken
        /// up into a block per statement, which is both unreadable and no more precise than one region covering the
        /// entire body.
        /// </remarks>
        internal const int BodyWideThreshold = 3;

        /// <summary>
        /// Groups the per-use-site fixes, so that "fix all occurrences" on any of them means the same thing.
        /// </summary>
        internal const string NarrowEquivalenceKey = nameof(IntroduceUnsafeContextCodeFixProvider) + ".Narrow";

        /// <summary>
        /// Identifies the body-wide fix, which "fix all occurrences" treats as a separate choice.
        /// </summary>
        internal const string BodyEquivalenceKey = nameof(IntroduceUnsafeContextCodeFixProvider) + ".Body";

        private static LocalizableString BlockTitle { get; } = Localize(nameof(Resources.IntroduceUnsafeBlockCodeFixTitle));

        private static LocalizableString ExpressionTitle { get; } = Localize(nameof(Resources.IntroduceUnsafeExpressionCodeFixTitle));

        private static LocalizableString BodyTitle { get; } = Localize(nameof(Resources.IntroduceUnsafeBodyCodeFixTitle));

        /// <summary>
        /// Whether the per-use-site regions of a body should be collapsed into one region covering the body.
        /// </summary>
        internal enum Consolidation
        {
            /// <summary>Every use site gets its own region.</summary>
            None,

            /// <summary>A body that would need <see cref="BodyWideThreshold"/> regions is wrapped whole.</summary>
            Threshold,

            /// <summary>Every body that has a use site is wrapped whole.</summary>
            Always,
        }

        public override ImmutableArray<string> FixableDiagnosticIds => UnsafeContextPlanner.DiagnosticIds;

        public override FixAllProvider GetFixAllProvider() => IntroduceUnsafeContextFixAllProvider.Instance;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Document document = context.Document;
            Diagnostic diagnostic = context.Diagnostics[0];
            CancellationToken cancellationToken = context.CancellationToken;

            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
                || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel
                || UnsafeContextPlanner.Plan(root, diagnostic.Location.SourceSpan, semanticModel) is not { } planned)
            {
                return;
            }

            // Fixing one use site keeps to that use site. Collapsing a body full of them is the second choice
            // here, and the default when the whole document is fixed at once.
            RegisterFix(context, diagnostic, planned);

            if (UnsafeContextPlanner.FindWrappableBody(planned.Target) is { } body)
                RegisterFix(context, diagnostic, new UnsafeContextFix(UnsafeContextKind.Body, body));
        }

        private static void RegisterFix(CodeFixContext context, Diagnostic diagnostic, UnsafeContextFix fix)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    GetTitle(fix.Kind).ToString(),
                    cancellationToken => UnsafeContextRewriter.ApplyAsync(context.Document, [fix], cancellationToken),
                    equivalenceKey: fix.Kind is UnsafeContextKind.Body ? BodyEquivalenceKey : NarrowEquivalenceKey),
                diagnostic);
        }

        private static LocalizableString GetTitle(UnsafeContextKind kind) =>
            kind switch
            {
                UnsafeContextKind.Expression => ExpressionTitle,
                UnsafeContextKind.Body => BodyTitle,
                _ => BlockTitle,
            };

        /// <summary>
        /// Plans every diagnostic in <paramref name="diagnostics"/> and applies the result to the document in one
        /// pass, so that overlapping and redundant edits are resolved against each other.
        /// </summary>
        internal static async Task<Document> FixAsync(
            Document document,
            IEnumerable<Diagnostic> diagnostics,
            Consolidation consolidation,
            CancellationToken cancellationToken)
        {
            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
                || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel)
            {
                return document;
            }

            ImmutableArray<UnsafeContextFix> planned = Consolidate(
                Plan(root, semanticModel, diagnostics),
                consolidation);

            return await UnsafeContextRewriter.ApplyAsync(document, planned, cancellationToken).ConfigureAwait(false);
        }

        private static ImmutableArray<UnsafeContextFix> Plan(
            SyntaxNode root,
            SemanticModel semanticModel,
            IEnumerable<Diagnostic> diagnostics) =>
            UnsafeContextPlanner.Coalesce(diagnostics
                .Select(diagnostic => UnsafeContextPlanner.Plan(root, diagnostic.Location.SourceSpan, semanticModel))
                .Where(static fix => fix.HasValue)
                .Select(static fix => fix!.Value));

        /// <summary>
        /// Replaces the individual edits of a body with a single body-wide block.
        /// </summary>
        private static ImmutableArray<UnsafeContextFix> Consolidate(
            ImmutableArray<UnsafeContextFix> fixes,
            Consolidation consolidation)
        {
            if (consolidation is Consolidation.None)
                return fixes;

            List<UnsafeContextFix> result = [];

            foreach (IGrouping<BlockSyntax?, UnsafeContextFix> body in
                fixes.GroupBy(static fix => UnsafeContextPlanner.FindWrappableBody(fix.Target)))
            {
                if (body.Key is { } block && (consolidation is Consolidation.Always || body.Count() >= BodyWideThreshold))
                    result.Add(new UnsafeContextFix(UnsafeContextKind.Body, block));
                else
                    result.AddRange(body);
            }

            return [.. result];
        }

        private static LocalizableResourceString Localize(string name) =>
            new(name, Resources.ResourceManager, typeof(Resources));
    }
}
#endif
