// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Creates block candidates for statements. A new block must keep every name visible where it was used, so the
    /// range is closed over the locals and labels it declares: locals used later are split out of the block, or the
    /// range grows to include their uses.
    /// </summary>
    internal static class BlockEditFactory
    {
        /// <summary>
        /// Returns the block edits covering at least <paramref name="first"/>..<paramref name="last"/>, narrowest first:
        /// one that splits declarations used after the block and one that grows over those uses instead.
        /// </summary>
        public static ImmutableArray<UnsafeContextEdit> CreateForRange(StatementList list, int first, int last, TargetSet targets, PlanningContext context)
        {
            var edits = ImmutableArray.CreateBuilder<UnsafeContextEdit>();

            // An adjacent placeholder block is extended rather than given a sibling.
            int extendedFirst = first > 0 && PlaceholderContexts.IsExtendableBlock(list[first - 1]) ? first - 1 : first;
            int extendedLast = last < list.Count - 1 && PlaceholderContexts.IsExtendableBlock(list[last + 1]) ? last + 1 : last;
            if ((extendedFirst != first || extendedLast != last)
                && TryCreate(list, extendedFirst, extendedLast, targets, allowSplitting: true, context) is { } extended)
            {
                edits.Add(extended);
            }

            UnsafeContextEdit? narrow = TryCreate(list, first, last, targets, allowSplitting: true, context);
            if (narrow is not null)
                edits.Add(narrow);

            if (narrow is null or StatementBlockEdit { HasSplits: true }
                && TryCreate(list, first, last, targets, allowSplitting: false, context) is { } grown)
            {
                edits.Add(grown);
            }

            return edits.ToImmutable();
        }

        /// <summary>Returns the block edit for the brace-less body of an <c>if</c>, <c>else</c>, loop, ... if any.</summary>
        public static ImmutableArray<UnsafeContextEdit> CreateForEmbedded(StatementSyntax statement, TargetSet targets, PlanningContext context)
        {
            // The statement's leading trivia moves inside the new braces with it.
            if (!CanEnclose(statement)
                || !DirectiveFacts.AreBalanced(context.Root, TextSpan.FromBounds(statement.FullSpan.Start, statement.Span.End)))
            {
                return [];
            }

            Coverage coverage = StatementShape.CountLeaves(statement, targets);
            bool isProportionate = coverage.IsBalanced && !StatementShape.CoversBodyOnlyForHeader(statement, targets);
            return EmbeddedBlockEdit.TryCreate(statement, context.Formatting, coverage, isProportionate) is { } edit ? [edit] : [];
        }

        /// <summary>
        /// Whether a statement may be moved into a new block. Lambdas and local functions would inherit the context,
        /// <c>yield</c> cannot appear in it, and existing contexts are never nested (placeholder wrappers are merged).
        /// </summary>
        public static bool CanEnclose(StatementSyntax statement) =>
            !statement.DescendantNodesAndSelf().Any(static n =>
                n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or YieldStatementSyntax or UnsafeStatementSyntax)
            && PlaceholderContexts.ContainsOnlyPlaceholderWrappers(statement);

        /// <summary>Counts the leaves a block range covers; an extended placeholder block only holds related leaves.</summary>
        public static Coverage CountRangeLeaves(StatementSyntax statement, TargetSet targets)
        {
            Coverage leaves = StatementShape.CountLeaves(statement, targets);
            return PlaceholderContexts.IsExtendableBlock(statement) ? new Coverage(leaves.Related + leaves.Unrelated, 0) : leaves;
        }

        /// <summary>
        /// Creates the block for <paramref name="first"/>..<paramref name="last"/> after closing the range over its
        /// declarations, or returns <see langword="null"/> when no valid block exists.
        /// </summary>
        private static UnsafeContextEdit? TryCreate(StatementList list, int first, int last, TargetSet targets, bool allowSplitting, PlanningContext context)
        {
            ImmutableDictionary<int, DeclarationSplit> splits;
            while (true)
            {
                // Placeholder blocks inside the range are merged into the new block.
                if (!GetRange(list, first, last).All(static s => PlaceholderContexts.IsExtendableBlock(s) || CanEnclose(s)))
                    return null;

                // A using declaration keeps its original disposal point: the end of the enclosing scope.
                if (last < list.Count - 1 && GetRange(list, first, last).Any(static s => s is LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not 0 }))
                {
                    last = list.Count - 1;
                    continue;
                }

                if (!TryCloseRange(list, ref first, ref last, allowSplitting, context, out splits, out bool grew))
                    return null;

                if (!grew)
                    break;
            }

            Coverage coverage = default;
            bool coversBodyOnlyForHeader = false;
            for (int i = first; i <= last; i++)
            {
                // Declarations kept outside the block are not counted.
                if (splits.TryGetValue(i, out DeclarationSplit? split) && split.Remaining.IsEmpty)
                    continue;

                coverage += CountRangeLeaves(list[i], targets);
                coversBodyOnlyForHeader |= StatementShape.CoversBodyOnlyForHeader(list[i], targets);
            }

            bool isProportionate = coverage.IsBalanced && !coversBodyOnlyForHeader;
            StatementSyntax firstWrapped = StatementBlockEdit.GetFirstWrappedStatement(list[first]);
            if (!context.Formatting.StartsLine(firstWrapped.GetFirstToken()) || !list[last].GetLastToken().EndsLine)
            {
                return first == 0 && last == list.Count - 1 && splits.IsEmpty && list.Owner is BlockSyntax block
                    ? SingleLineBlockEdit.TryCreate(block, context.Formatting, coverage, isProportionate)
                    : null;
            }

            if (!DirectiveFacts.AreBalanced(context.Root, TextSpan.FromBounds(firstWrapped.SpanStart, list[last].Span.End)))
                return null;

            return new StatementBlockEdit(list, first, last, splits, coverage, isProportionate);
        }

        /// <summary>
        /// Splits or grows over the declarations of the range that are referenced outside it. Returns
        /// <see langword="false"/> when a reference cannot be included, e.g. from another switch section.
        /// </summary>
        private static bool TryCloseRange(
            StatementList list,
            ref int first,
            ref int last,
            bool allowSplitting,
            PlanningContext context,
            out ImmutableDictionary<int, DeclarationSplit> splits,
            out bool grew)
        {
            var splitBuilder = ImmutableDictionary.CreateBuilder<int, DeclarationSplit>();
            splits = splitBuilder.ToImmutable();
            grew = false;
            int newFirst = first, newLast = last;
            if (HidesInactiveDeclarations(list, first, last))
                return false;

            Dictionary<ISymbol, List<int>> references = FindReferencesOutside(list, first, last, context);
            List<ISymbol> unsplit = [];
            if (allowSplitting)
            {
                foreach (var group in references.Keys.OfType<ILocalSymbol>().GroupBy(local => GetDeclaringStatementIndex(list, local)))
                {
                    // A split declaration takes the comments above it out of the block; directives must stay in place.
                    int index = group.Key;
                    if (index >= first && index <= last
                        && (index != first || list[index] is not LabeledStatementSyntax)
                        && (index == first || !list[index].GetLeadingTrivia().Any(static t => t.IsDirective))
                        && DeclarationSplitter.TrySplit(list[index], group, context) is { } split)
                    {
                        splitBuilder[index] = split;
                    }
                    else
                    {
                        unsplit.AddRange(group);
                    }
                }

                unsplit.AddRange(references.Keys.Where(static s => s is not ILocalSymbol));
            }
            else
            {
                unsplit.AddRange(references.Keys);
            }

            foreach (ISymbol symbol in unsplit)
            {
                List<int> indices = references[symbol];
                if (indices.Contains(-1))
                    return false;

                newFirst = Math.Min(newFirst, indices.Min());
                newLast = Math.Max(newLast, indices.Max());
            }

            grew = newFirst != first || newLast != last;
            first = newFirst;
            last = newLast;
            splits = splitBuilder.ToImmutable();
            return true;
        }

        /// <summary>
        /// Finds the locals and labels declared in the range that are referenced outside it, with the indices of the
        /// referencing statements: -1 for references the range cannot grow over, i.e. from another switch section or
        /// an inactive <c>#if</c> arm.
        /// </summary>
        private static Dictionary<ISymbol, List<int>> FindReferencesOutside(StatementList list, int first, int last, PlanningContext context)
        {
            SemanticModel model = context.Model;
            Dictionary<string, HashSet<ISymbol>> declared = [];
            foreach (StatementSyntax statement in GetRange(list, first, last))
            {
                foreach (SyntaxNode node in statement.DescendantNodesAndSelf())
                {
                    ISymbol? symbol = node switch
                    {
                        VariableDeclaratorSyntax variable => model.GetDeclaredSymbol(variable, context.CancellationToken),
                        SingleVariableDesignationSyntax designation => model.GetDeclaredSymbol(designation, context.CancellationToken),
                        // A leading label stays outside the block.
                        LabeledStatementSyntax labeled when labeled != list[first] => model.GetDeclaredSymbol(labeled, context.CancellationToken),
                        _ => null,
                    };

                    if (symbol is not null)
                    {
                        if (!declared.TryGetValue(symbol.Name, out HashSet<ISymbol>? symbols))
                            declared[symbol.Name] = symbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

                        symbols.Add(symbol);
                    }
                }
            }

            var references = new Dictionary<ISymbol, List<int>>(SymbolEqualityComparer.Default);
            if (declared.Count == 0)
                return references;

            foreach (var (statement, index) in list.StatementsInScope)
            {
                if (index >= first && index <= last)
                    continue;

                foreach (IdentifierNameSyntax identifier in statement.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    if (!declared.TryGetValue(identifier.Identifier.ValueText, out HashSet<ISymbol>? candidates))
                        continue;

                    ISymbol? symbol = model.GetSymbolInfo(identifier, context.CancellationToken).Symbol;
                    if (symbol is not null && candidates.Contains(symbol))
                        AddReference(references, symbol, index);
                }
            }

            // Uses in inactive `#if` arms are invisible to the semantic model. A lexical match is a reference the range
            // cannot grow over (-1), so the declaration must be split out or the block declined. Labels can also be
            // targeted from before the range.
            TextSpan range = GetRangeSpan(list, first, last);
            HashSet<string> usedAfter = [.. DirectiveFacts.GetDisabledIdentifiers(list.ScopeNode, TextSpan.FromBounds(range.End, list.ScopeNode.Span.End))];
            HashSet<string> usedBefore = [.. DirectiveFacts.GetDisabledIdentifiers(list.ScopeNode, TextSpan.FromBounds(list.ScopeNode.SpanStart, range.Start))];
            foreach (var pair in declared)
            {
                foreach (ISymbol symbol in pair.Value)
                {
                    if (usedAfter.Contains(pair.Key) || (symbol is ILabelSymbol && usedBefore.Contains(pair.Key)))
                        AddReference(references, symbol, -1);
                }
            }

            return references;
        }

        private static IEnumerable<StatementSyntax> GetRange(StatementList list, int first, int last) =>
            list.Statements.Skip(first).Take(last - first + 1);

        private static void AddReference(Dictionary<ISymbol, List<int>> references, ISymbol symbol, int index)
        {
            if (!references.TryGetValue(symbol, out List<int>? indices))
                references[symbol] = indices = [];

            indices.Add(index);
        }

        /// <summary>The text moved into the block: from the first statement's first token to the last statement's end.</summary>
        private static TextSpan GetRangeSpan(StatementList list, int first, int last) =>
            TextSpan.FromBounds(list[first].SpanStart, list[last].Span.End);

        /// <summary>
        /// Whether an inactive <c>#if</c> arm inside the range declares a name that appears after it. In
        /// that configuration the new block would hide the declaration, which no single-configuration check can see.
        /// </summary>
        private static bool HidesInactiveDeclarations(StatementList list, int first, int last)
        {
            SyntaxNode scope = list.ScopeNode;
            TextSpan range = GetRangeSpan(list, first, last);
            HashSet<string> names = [.. DirectiveFacts.GetDisabledDeclarations(scope, range)];
            if (names.Count == 0)
                return false;

            var after = TextSpan.FromBounds(range.End, scope.Span.End);
            return scope.DescendantTokens(after).Any(t => t.IsKind(SyntaxKind.IdentifierToken) && names.Contains(t.ValueText))
                || DirectiveFacts.GetDisabledIdentifiers(scope, after).Any(names.Contains);
        }

        private static int GetDeclaringStatementIndex(StatementList list, ILocalSymbol local) =>
            local.DeclaringSyntaxReferences.FirstOrDefault() is { } reference
                ? list.IndexOfStatementContaining(reference.Span)
                : -1;
    }
}
