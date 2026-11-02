// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if UNSAFE_MIGRATION
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Fix-all for the unsafe-v2 migration fixers. For each diagnostic in scope it applies the action registered with
    /// the requested equivalence key, or the first registered action when no key was requested, and merges the edits.
    /// </summary>
    /// <remarks>
    /// The batch fixer only applies actions whose key matches the one taken from the first diagnostic's action.
    /// <c>dotnet format</c> takes that key from an arbitrary diagnostic, so when that diagnostic has no fix (for
    /// example an <c>IL5005</c> on a pointer signature, which has to stay <c>unsafe</c>) the key is null and the batch
    /// fixer silently does nothing for the whole pass.
    /// </remarks>
    internal sealed class UnsafeMigrationFixAllProvider : FixAllProvider
    {
        internal static readonly UnsafeMigrationFixAllProvider Instance = new();

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() =>
            [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution];

        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            CancellationToken cancellationToken = fixAllContext.CancellationToken;
            Solution solution = fixAllContext.Solution;
            var accepted = new Dictionary<DocumentId, List<TextChange>>();

            foreach (Document document in GetDocuments(fixAllContext))
            {
                ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                foreach (Diagnostic diagnostic in diagnostics.OrderBy(static diagnostic => diagnostic.Location.SourceSpan.Start))
                {
                    if (await GetChangedSolutionAsync(fixAllContext, document, diagnostic).ConfigureAwait(false) is { } changed)
                        await AddChangesAsync(solution, changed, accepted, cancellationToken).ConfigureAwait(false);
                }
            }

            if (accepted.Count == 0)
                return null;

            Solution result = solution;
            foreach (KeyValuePair<DocumentId, List<TextChange>> pair in accepted)
            {
                SourceText text = await solution.GetDocument(pair.Key)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
                result = result.WithDocumentText(pair.Key, text.WithChanges(pair.Value.OrderBy(static change => change.Span.Start)));
            }

            return CodeAction.Create(
                "Fix all occurrences",
                _ => Task.FromResult(result),
                fixAllContext.CodeActionEquivalenceKey);
        }

        private static IEnumerable<Document> GetDocuments(FixAllContext fixAllContext) =>
            fixAllContext.Scope switch
            {
                FixAllScope.Document when fixAllContext.Document is { } document => [document],
                FixAllScope.Project => fixAllContext.Project.Documents,
                FixAllScope.Solution => fixAllContext.Solution.Projects
                    .Where(project => project.Language == fixAllContext.Project.Language)
                    .SelectMany(static project => project.Documents),
                _ => [],
            };

        private static async Task<Solution?> GetChangedSolutionAsync(FixAllContext fixAllContext, Document document, Diagnostic diagnostic)
        {
            string? key = fixAllContext.CodeActionEquivalenceKey;
            CodeAction? first = null;
            CodeAction? matching = null;
            var context = new CodeFixContext(
                document,
                diagnostic,
                (action, _) =>
                {
                    first ??= action;
                    if (matching is null && key is not null && action.EquivalenceKey == key)
                        matching = action;
                },
                fixAllContext.CancellationToken);
            await fixAllContext.CodeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            if ((key is null ? first : matching) is not { } chosen)
                return null;

            foreach (CodeActionOperation operation in await chosen.GetOperationsAsync(fixAllContext.CancellationToken).ConfigureAwait(false))
            {
                if (operation is ApplyChangesOperation apply)
                    return apply.ChangedSolution;
            }

            return null;
        }

        /// <summary>
        /// Accepts one action's edits unless any of them conflicts with an edit already accepted. Identical edits, such
        /// as two diagnostics asking for the same modifier on one declaration, are applied once.
        /// </summary>
        private static async Task AddChangesAsync(
            Solution original,
            Solution changed,
            Dictionary<DocumentId, List<TextChange>> accepted,
            CancellationToken cancellationToken)
        {
            var pending = new List<(DocumentId Id, List<TextChange> Changes)>();
            foreach (ProjectChanges projectChanges in changed.GetChanges(original).GetProjectChanges())
            {
                foreach (DocumentId id in projectChanges.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
                {
                    Document newDocument = changed.GetDocument(id)!;
                    Document oldDocument = original.GetDocument(id)!;
                    List<TextChange> changes = [.. await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false)];
                    List<TextChange> existing = accepted.TryGetValue(id, out List<TextChange>? list) ? list : [];

                    changes.RemoveAll(existing.Contains);
                    if (changes.Any(change => existing.Any(other => Conflicts(change, other))))
                        return;

                    pending.Add((id, changes));
                }
            }

            foreach ((DocumentId id, List<TextChange> changes) in pending)
            {
                if (!accepted.TryGetValue(id, out List<TextChange>? list))
                    accepted[id] = list = [];
                list.AddRange(changes);
            }
        }

        private static bool Conflicts(TextChange change, TextChange other) =>
            change.Span.OverlapsWith(other.Span)
            || (change.Span.Start == other.Span.Start && (change.Span.IsEmpty || other.Span.IsEmpty));
    }
}
#endif
