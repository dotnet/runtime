// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>The fixed document and the form of the context covering the requested operation.</summary>
    internal sealed record UnsafeContextFixResult(Document Document, UnsafeContextForm Form);

    /// <summary>
    /// Introduces unsafe contexts for the requested diagnostics of one document. Every member containing a requested
    /// diagnostic is planned as a whole, so its operations share contexts however they were requested. Candidates are
    /// validated in rounds on one forked compilation per round; failing regions fall back to their next candidate and
    /// regions without a valid candidate are left unchanged.
    /// </summary>
    internal static class UnsafeContextDocumentFixer
    {
        /// <summary>Fixes the members containing <paramref name="diagnostics"/>; returns <see langword="null"/> if nothing could be fixed.</summary>
        /// <param name="focus">For a single code action, the diagnostic whose region is applied; <see langword="null"/> for Fix All.</param>
        public static async Task<UnsafeContextFixResult?> FixAsync(
            Document document,
            IEnumerable<Diagnostic> diagnostics,
            UnsafeContextPolicy policy,
            Diagnostic? focus,
            CancellationToken cancellationToken)
        {
            SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SemanticModel? model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || model is null || !UnsafeContextFacts.IsEnabled(document, root, model.Compilation))
                return null;

            // Every operation of a member is planned, so that its contexts are shared however they were requested.
            Dictionary<MemberDeclarationSyntax, ImmutableArray<Diagnostic>> baseline = [];
            List<UnsafeTarget> targets = [];
            foreach (MemberDeclarationSyntax member in diagnostics.Select(d => UnsafeTarget.Create(root, d)?.Scope.Member).OfType<MemberDeclarationSyntax>().Distinct())
            {
                ImmutableArray<Diagnostic> memberDiagnostics = UnsafeContextSemantics.GetDiagnostics(model, member, cancellationToken);
                baseline[member] = memberDiagnostics;
                targets.AddRange(memberDiagnostics
                    .Select(d => UnsafeTarget.Create(root, d))
                    .OfType<UnsafeTarget>()
                    .Where(t => t.Scope.Member == member));
            }

            if (targets.Count == 0)
                return null;

            PlanningContext context = new(root, model, policy, cancellationToken);
            UnsafeContextValidator validator = new(document, root, model.Compilation, context, baseline);
            List<UnsafeContextRegion> regions = [.. new UnsafeContextPlanner(context).Plan(targets)];
            if (Converge(regions, validator, cancellationToken) is not var (newRoot, applied))
                return null;

            if (focus is null)
                return new UnsafeContextFixResult(document.WithSyntaxRoot(newRoot), applied[0].CurrentEdit!.Form);

            // The requested operation is fixed by its own region or by a block that subsumed it.
            List<UnsafeContextRegion> focused = [.. applied.Where(r => r.Targets.Any(t => t.Diagnostic.Id == focus.Id && t.Diagnostic.Location.SourceSpan == focus.Location.SourceSpan))];
            if (focused.Count == 0)
                focused = [.. applied.Where(r => r.CurrentEdit!.Form == UnsafeContextForm.Block && r.CurrentEdit.Span.Contains(focus.Location.SourceSpan)).Take(1)];

            if (focused.Count == 0)
                return null;

            UnsafeContextForm form = focused[0].CurrentEdit!.Form;
            if (focused.Count < applied.Count)
            {
                SyntaxNode focusedRoot = validator.Apply(focused);
                if (validator.Validate(focusedRoot, focused).Count == 0)
                    return new UnsafeContextFixResult(document.WithSyntaxRoot(focusedRoot), form);
            }

            return new UnsafeContextFixResult(document.WithSyntaxRoot(newRoot), form);
        }

        /// <summary>
        /// Validates the regions until every remaining region has a valid candidate. Returns the edited root and the
        /// applied regions, or <see langword="null"/> when no region could be fixed. Terminates because every failure
        /// moves a region to a later candidate and each refinement is expanded at most once.
        /// </summary>
        private static (SyntaxNode Root, List<UnsafeContextRegion> Applied)? Converge(List<UnsafeContextRegion> regions, UnsafeContextValidator validator, CancellationToken cancellationToken)
        {
            Normalize(regions);
            while (regions.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                List<UnsafeContextRegion> applied = SelectApplicable(regions);
                SyntaxNode newRoot = validator.Apply(applied);
                HashSet<UnsafeContextRegion> failed = validator.Validate(newRoot, applied);
                if (failed.Count == 0)
                    return (newRoot, applied);

                // A region may already be gone because an earlier failure fell back to its ancestor.
                foreach (UnsafeContextRegion region in failed.Where(regions.Contains))
                    Fail(regions, region);

                Normalize(regions);
            }

            return null;
        }

        /// <summary>
        /// Returns the regions to apply in this round. A block that covers another region (e.g. after growing over a
        /// <c>ref</c> local's uses) subsumes it; a block that only partially overlaps another region falls back to its
        /// next candidate. Wrappers may nest around regions of lambdas they do not cover.
        /// </summary>
        private static List<UnsafeContextRegion> SelectApplicable(List<UnsafeContextRegion> regions)
        {
            while (true)
            {
                List<UnsafeContextRegion> applied = [];
                UnsafeContextRegion? conflict = null;
                foreach (UnsafeContextRegion region in regions.OrderByDescending(static r => r.CurrentEdit!.Span.Length))
                {
                    TextSpan span = region.CurrentEdit!.Span;
                    bool subsumed = false;
                    foreach (UnsafeContextRegion other in applied)
                    {
                        UnsafeContextEdit otherEdit = other.CurrentEdit!;
                        if (!otherEdit.Span.OverlapsWith(span))
                            continue;

                        if (!otherEdit.Span.Contains(span))
                        {
                            conflict = otherEdit.Form == UnsafeContextForm.Block ? other : region;
                            break;
                        }

                        subsumed |= otherEdit.Form == UnsafeContextForm.Block;
                    }

                    if (conflict is not null)
                        break;

                    if (!subsumed)
                        applied.Add(region);
                }

                if (conflict is null)
                    return applied;

                Fail(regions, conflict);
                Normalize(regions);
            }
        }

        /// <summary>Expands refinements and drops regions without candidates, until every region has an edit.</summary>
        private static void Normalize(List<UnsafeContextRegion> regions)
        {
            for (int i = 0; i < regions.Count;)
            {
                UnsafeContextRegion region = regions[i];
                if (region.Current is null)
                {
                    Fail(regions, region);
                    i = 0;
                }
                else if (region.Current.Refine is { } refine)
                {
                    regions.RemoveAt(i);
                    regions.AddRange(refine(region));
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Moves a failing region to its next candidate. A region out of candidates is dropped; if it came from a
        /// refinement whose parent has further candidates, the parent replaces all of its refined regions.
        /// </summary>
        private static void Fail(List<UnsafeContextRegion> regions, UnsafeContextRegion region)
        {
            if (region.MoveNext())
                return;

            regions.Remove(region);
            for (UnsafeContextRegion? ancestor = region.Parent; ancestor is not null; ancestor = ancestor.Parent)
            {
                if (!ancestor.HasNext)
                    continue;

                regions.RemoveAll(r => r.IsDescendantOf(ancestor));
                ancestor.MoveNext();
                regions.Add(ancestor);
                return;
            }
        }
    }
}
