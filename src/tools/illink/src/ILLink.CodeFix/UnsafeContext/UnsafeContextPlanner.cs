// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Groups the operations of each scope into regions and orders each region's candidates by the coverage rule:
    /// proportionate blocks first, then the alternatives (wrappers or finer regions), then disproportionate blocks
    /// with the fewest unrelated leaves.
    /// </summary>
    internal sealed class UnsafeContextPlanner(PlanningContext context)
    {
        /// <summary>Returns the regions for <paramref name="targets"/>, one or more per scope.</summary>
        public ImmutableArray<UnsafeContextRegion> Plan(IEnumerable<UnsafeTarget> targets)
        {
            var regions = ImmutableArray.CreateBuilder<UnsafeContextRegion>();
            foreach (var group in targets.GroupBy(static t => t.Scope.Body))
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                regions.AddRange(PlanScope(group.First().Scope, [.. group]));
            }

            return regions.ToImmutable();
        }

        private IEnumerable<UnsafeContextRegion> PlanScope(UnsafeScope scope, ImmutableArray<UnsafeTarget> targets)
        {
            if (scope.Kind != UnsafeScopeKind.Block)
                return [PlanExpressionScope(scope, targets)];

            // Placeholder wrappers count as operations: a block covering them replaces them.
            TargetSet set = new([.. targets.Select(static t => t.Span), .. PlaceholderContexts.GetPlaceholderWrapperSpans(scope.Body)]);
            StatementList list = new((BlockSyntax)scope.Body);
            if (context.Policy == UnsafeContextPolicy.BodyWide && CreateBodyWideRegion(scope, list, targets, set) is { } bodyWide)
                return [bodyWide];

            return PlanList(scope, list, targets, set, parent: null);
        }

        /// <summary>
        /// Expression bodies and initializers keep their form: the narrowest wrapper first. An expression body is
        /// converted to a block body only when no wrapper is valid.
        /// </summary>
        private UnsafeContextRegion PlanExpressionScope(UnsafeScope scope, ImmutableArray<UnsafeTarget> targets)
        {
            List<RegionCandidate> candidates = [.. ExpressionWrapPlanner.Create(targets, scope, scope.Body.Parent!, context).Select(RegionCandidate.ForEdit)];

            // A block body would put nested lambdas and local functions in the unsafe context, would separate
            // directives between `=>` and the end of the member from their counterparts, and would nest any wrapper.
            if (scope.Kind == UnsafeScopeKind.ExpressionBody
                && ExpressionBodyBlockEdit.CanConvert(scope.Owner)
                && !scope.Body.DescendantNodesAndSelf().Any(static n => n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax || UnsafeContextFacts.IsUnsafeExpression(n))
                && !scope.Owner.DescendantTrivia(TextSpan.FromBounds(scope.Body.GetFirstToken().GetPreviousToken().SpanStart, scope.Owner.Span.End)).Any(static t => t.IsDirective))
            {
                bool asStatement = UnsafeContextSemantics.RequiresStatementExpressionBody(scope.Owner, context.Model, context.CancellationToken);
                int position = scope.Owner is LambdaExpressionSyntax ? scope.Owner.SpanStart : scope.Owner.FirstTokenAfterAttributes.SpanStart;
                string indentation = context.Formatting.GetIndentation(position);
                candidates.Add(RegionCandidate.ForEdit(new ExpressionBodyBlockEdit(scope.Owner, (ExpressionSyntax)scope.Body, asStatement, indentation)));
            }

            return new UnsafeContextRegion(targets, [.. candidates], parent: null);
        }

        /// <summary>Creates a region whose first candidates wrap the whole body, refining to the default plan.</summary>
        private UnsafeContextRegion? CreateBodyWideRegion(UnsafeScope scope, StatementList list, ImmutableArray<UnsafeTarget> targets, TargetSet set)
        {
            // Trailing local functions stay outside the block.
            int last = list.Count - 1;
            while (last >= 0 && list[last] is LocalFunctionStatementSyntax)
                last--;

            if (last < 0)
                return null;

            var blocks = BlockEditFactory.CreateForRange(list, 0, last, set, context);
            return blocks.IsEmpty
                ? null
                : new UnsafeContextRegion(
                    targets,
                    [.. blocks.Select(RegionCandidate.ForEdit), RegionCandidate.ForRefinement(region => PlanList(scope, list, targets, set, region))],
                    parent: null);
        }

        /// <summary>
        /// Plans the operations of a statement list. Statements that own operations (simple statements and compound
        /// statements with operations in their headers) are anchors; adjacent anchors, and anchors separated by a gap
        /// that keeps the merged block proportionate, form one region. Compound statements with operations only in
        /// their bodies are planned inside those bodies unless a merged region covers them.
        /// </summary>
        private IEnumerable<UnsafeContextRegion> PlanList(UnsafeScope scope, StatementList list, ImmutableArray<UnsafeTarget> targets, TargetSet set, UnsafeContextRegion? parent)
        {
            var byStatement = targets
                .GroupBy(t => list.IndexOfStatementContaining(t.Span))
                .Where(static g => g.Key >= 0)
                .OrderBy(static g => g.Key)
                .ToDictionary(static g => g.Key, static g => g.ToImmutableArray());

            List<UnsafeContextRegion> regions = [];
            HashSet<int> covered = [];
            foreach (var (first, last) in MergeAnchors(list, byStatement, set))
            {
                ImmutableArray<UnsafeTarget> rangeTargets = [.. byStatement.Where(p => p.Key >= first && p.Key <= last).SelectMany(static p => p.Value)];
                covered.UnionWith(Enumerable.Range(first, last - first + 1));
                regions.Add(first == last
                    ? PlanStatement(scope, list, list[first], rangeTargets, set, parent)
                    : PlanRange(scope, list, first, last, rangeTargets, set, parent));
            }

            foreach (var pair in byStatement.Where(p => !covered.Contains(p.Key)))
                regions.AddRange(PlanBodies(scope, list[pair.Key], pair.Value, set, parent));

            return regions;
        }

        /// <summary>Returns the ranges of anchor statements, merging each anchor into the previous range when allowed.</summary>
        private IEnumerable<(int First, int Last)> MergeAnchors(StatementList list, Dictionary<int, ImmutableArray<UnsafeTarget>> byStatement, TargetSet set)
        {
            (int First, int Last)? range = null;
            foreach (int index in byStatement.Keys.Where(i => StatementShape.HasOwnOperations(list[i], set)).OrderBy(static i => i))
            {
                if (range is (int first, int last) && CanMerge(list, first, last, index, set))
                {
                    range = (first, index);
                    continue;
                }

                if (range is { } previous)
                    yield return previous;

                range = (index, index);
            }

            if (range is { } final)
                yield return final;
        }

        /// <summary>
        /// Whether the anchor at <paramref name="next"/> joins the range: both sides must be enclosable on their own,
        /// and a gap must need no declaration splitting or closure growth and keep the block proportionate.
        /// </summary>
        private bool CanMerge(StatementList list, int first, int last, int next, TargetSet set)
        {
            if (context.Policy == UnsafeContextPolicy.ExpressionFirst || !IsMergeable(list[last], set) || !IsMergeable(list[next], set))
                return false;

            Coverage coverage = default;
            for (int i = first; i <= next; i++)
            {
                // Placeholder blocks in the gap are merged.
                StatementSyntax statement = list[i];
                bool isGap = i > last && i < next;
                if (isGap
                    && !PlaceholderContexts.IsExtendableBlock(statement)
                    && (statement is LocalDeclarationStatementSyntax or LabeledStatementSyntax || !BlockEditFactory.CanEnclose(statement)))
                {
                    return false;
                }

                coverage += BlockEditFactory.CountRangeLeaves(statement, set);
            }

            return coverage.IsBalanced;
        }

        private static bool IsMergeable(StatementSyntax statement, TargetSet set) =>
            BlockEditFactory.CanEnclose(statement)
            && !StatementShape.CoversBodyOnlyForHeader(statement, set)
            && StatementShape.CountLeaves(statement, set).IsBalanced;

        /// <summary>Plans a merged range: one block, refining to one region per statement.</summary>
        private UnsafeContextRegion PlanRange(UnsafeScope scope, StatementList list, int first, int last, ImmutableArray<UnsafeTarget> targets, TargetSet set, UnsafeContextRegion? parent)
        {
            var blocks = BlockEditFactory.CreateForRange(list, first, last, set, context);
            var refine = RegionCandidate.ForRefinement(region => targets
                .GroupBy(t => list.IndexOfStatementContaining(t.Span))
                .SelectMany(g => StatementShape.HasOwnOperations(list[g.Key], set)
                    ? [PlanStatement(scope, list, list[g.Key], [.. g], set, region)]
                    : PlanBodies(scope, list[g.Key], [.. g], set, region)));

            return new UnsafeContextRegion(targets, Order(blocks, [refine]), parent);
        }

        /// <summary>
        /// Plans a statement that owns operations. A simple statement or a compound statement with operations only in
        /// its headers falls back to <c>unsafe(...)</c> wrappers; a compound statement with operations in its bodies
        /// falls back to planning its header and bodies separately.
        /// </summary>
        private UnsafeContextRegion PlanStatement(UnsafeScope scope, StatementList? list, StatementSyntax statement, ImmutableArray<UnsafeTarget> targets, TargetSet set, UnsafeContextRegion? parent)
        {
            var blocks = list is not null && list.Statements.IndexOf(statement) is var index
                ? BlockEditFactory.CreateForRange(list, index, index, set, context)
                : BlockEditFactory.CreateForEmbedded(statement, set, context);

            StatementShape shape = StatementShape.Of(statement);
            bool hasBodyOperations = targets.Any(t => shape.AnyBodyContains(t.Span));
            IEnumerable<RegionCandidate> alternatives = hasBodyOperations || GroupHeaderTargets(statement, targets).Count() > 1
                ? [RegionCandidate.ForRefinement(region => PlanHeaderAndBodies(scope, statement, targets, set, region))]
                : ExpressionWrapPlanner.Create(targets, scope, statement, context).Select(RegionCandidate.ForEdit);

            return new UnsafeContextRegion(targets, Order(blocks, alternatives), parent);
        }

        /// <summary>Plans one wrapper region per header group, then the bodies.</summary>
        private IEnumerable<UnsafeContextRegion> PlanHeaderAndBodies(UnsafeScope scope, StatementSyntax statement, ImmutableArray<UnsafeTarget> targets, TargetSet set, UnsafeContextRegion parent)
        {
            StatementShape shape = StatementShape.Of(statement);
            foreach (var group in GroupHeaderTargets(statement, targets.Where(t => !shape.AnyBodyContains(t.Span))))
            {
                var wrappers = ExpressionWrapPlanner.Create([.. group], scope, statement, context);
                yield return new UnsafeContextRegion([.. group], [.. wrappers.Select(RegionCandidate.ForEdit)], parent);
            }

            foreach (UnsafeContextRegion region in PlanBodies(scope, statement, targets, set, parent))
                yield return region;
        }

        /// <summary>
        /// Groups header operations by clause: switch-label guards and catch filters each get their own wrapper, other
        /// headers belong to the statement and so share one wrapper.
        /// </summary>
        private static IEnumerable<IGrouping<SyntaxNode, UnsafeTarget>> GroupHeaderTargets(StatementSyntax statement, IEnumerable<UnsafeTarget> targets) =>
            targets.GroupBy(t => t.Node.AncestorsAndSelf()
                .TakeWhile(a => a != statement)
                .FirstOrDefault(static a => a is CasePatternSwitchLabelSyntax or CatchClauseSyntax) ?? statement);

        /// <summary>Plans the operations inside the bodies of a compound statement.</summary>
        private IEnumerable<UnsafeContextRegion> PlanBodies(UnsafeScope scope, StatementSyntax statement, ImmutableArray<UnsafeTarget> targets, TargetSet set, UnsafeContextRegion? parent)
        {
            foreach (SyntaxNode body in StatementShape.Of(statement).Bodies)
            {
                ImmutableArray<UnsafeTarget> bodyTargets = [.. targets.Where(t => StatementShape.BodyContains(body, t.Span))];
                if (bodyTargets.IsEmpty)
                    continue;

                IEnumerable<UnsafeContextRegion> regions = StatementList.ForContainer(body) is { } list
                    ? PlanList(scope, list, bodyTargets, set, parent)
                    : StatementShape.HasOwnOperations((StatementSyntax)body, set)
                        ? [PlanStatement(scope, list: null, (StatementSyntax)body, bodyTargets, set, parent)]
                        : PlanBodies(scope, (StatementSyntax)body, bodyTargets, set, parent);

                foreach (UnsafeContextRegion region in regions)
                    yield return region;
            }
        }

        /// <summary>Orders candidates by the coverage rule, or alternatives first for <see cref="UnsafeContextPolicy.ExpressionFirst"/>.</summary>
        private ImmutableArray<RegionCandidate> Order(ImmutableArray<UnsafeContextEdit> blocks, IEnumerable<RegionCandidate> alternatives)
        {
            var proportionate = blocks.Where(static b => b.IsProportionate).Select(RegionCandidate.ForEdit);
            var others = blocks.Where(static b => !b.IsProportionate).OrderBy(static b => b.UnrelatedLeaves).Select(RegionCandidate.ForEdit);
            return context.Policy == UnsafeContextPolicy.ExpressionFirst
                ? [.. alternatives, .. proportionate, .. others]
                : [.. proportionate, .. alternatives, .. others];
        }
    }
}
