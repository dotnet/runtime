// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Fixes every use site in a document in a single pass.
    /// </summary>
    /// <remarks>
    /// The batch fixer computes each edit against the original document and then merges the text changes, which
    /// cannot express the two decisions this fixer has to make across diagnostics: one unsafe region usually
    /// covers several use sites, and a body that needs too many regions is better wrapped whole.
    /// </remarks>
    internal sealed class IntroduceUnsafeContextFixAllProvider : FixAllProvider
    {
        internal static IntroduceUnsafeContextFixAllProvider Instance { get; } = new();

        private IntroduceUnsafeContextFixAllProvider()
        {
        }

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() =>
            [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution];

        public override async Task<CodeAction?> GetFixAsync(FixAllContext context)
        {
            ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(context).ConfigureAwait(false);
            if (diagnostics.IsEmpty)
                return null;

            return CodeAction.Create(
                Title.ToString(),
                cancellationToken => FixAllAsync(context, diagnostics, cancellationToken),
                nameof(IntroduceUnsafeContextFixAllProvider));
        }

        private static LocalizableString Title { get; } = new LocalizableResourceString(
            nameof(Resources.IntroduceUnsafeContextFixAllTitle), Resources.ResourceManager, typeof(Resources));

        private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(FixAllContext context)
        {
            switch (context.Scope)
            {
                case FixAllScope.Document when context.Document is { } document:
                    return await context.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);

                case FixAllScope.Project:
                    return await context.GetAllDiagnosticsAsync(context.Project).ConfigureAwait(false);

                case FixAllScope.Solution:
                    ImmutableArray<Diagnostic>[] perProject = await Task.WhenAll(
                        context.Solution.Projects.Select(context.GetAllDiagnosticsAsync)).ConfigureAwait(false);
                    return [.. perProject.SelectMany(static diagnostics => diagnostics)];

                default:
                    return [];
            }
        }

        private static async Task<Solution> FixAllAsync(
            FixAllContext context,
            ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            Solution solution = context.Solution;

            // Choosing the body-wide fix and then applying it everywhere means every body; choosing the
            // per-use-site fix and applying it everywhere means every body that is not overrun by use sites.
            IntroduceUnsafeContextCodeFixProvider.Consolidation consolidation =
                context.CodeActionEquivalenceKey == IntroduceUnsafeContextCodeFixProvider.BodyEquivalenceKey
                    ? IntroduceUnsafeContextCodeFixProvider.Consolidation.Always
                    : IntroduceUnsafeContextCodeFixProvider.Consolidation.Threshold;

            foreach (IGrouping<SyntaxTree?, Diagnostic> group in diagnostics.GroupBy(static d => d.Location.SourceTree))
            {
                if (group.Key is not { } tree || context.Solution.GetDocument(tree) is not { Id: { } id })
                    continue;

                // The document has to be taken from the solution as it is being built up, not from the one the
                // diagnostics were computed against, so that earlier edits are carried forward.
                if (solution.GetDocument(id) is not { } document)
                    continue;

                Document fixedDocument = await IntroduceUnsafeContextCodeFixProvider
                    .FixAsync(document, group, consolidation, cancellationToken)
                    .ConfigureAwait(false);

                solution = fixedDocument.Project.Solution;
            }

            return solution;
        }
    }
}
#endif
