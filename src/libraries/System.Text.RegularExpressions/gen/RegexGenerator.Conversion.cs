// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;
using SourceGenerators;

namespace System.Text.RegularExpressions.Generator
{
    public partial class RegexGenerator
    {
        /// <summary>
        /// Converts mutable <see cref="RegexTree"/> and <see cref="AnalysisResults"/>
        /// into an immutable, structurally equatable <see cref="RegexTreeSpec"/> snapshot suitable for
        /// incremental caching in the Roslyn source generator pipeline.
        /// </summary>
        private static RegexTreeSpec CreateRegexTreeSpec(RegexTree tree, AnalysisResults analysis)
        {
            RegexNodeSpec rootSpec = ConvertNode(tree.Root, analysis);

            return new RegexTreeSpec(
                Root: rootSpec,
                Options: tree.Options,
                CaptureCount: tree.CaptureCount,
                CultureName: tree.Culture?.Name,
                CaptureNames: tree.CaptureNames?.ToImmutableEquatableArray(),
                CaptureNameToNumberMapping: tree.CaptureNameToNumberMapping?.ToImmutableEquatableDictionary<string, int>(),
                CaptureNumberSparseMapping: tree.CaptureNumberSparseMapping?.ToImmutableEquatableDictionary<int, int>(),
                FindOptimizations: ConvertFindOptimizations(tree.FindOptimizations, analysis),
                HasIgnoreCase: analysis.HasIgnoreCase,
                HasRightToLeft: analysis.HasRightToLeft);
        }

        /// <summary>Recursively converts a <see cref="RegexNode"/> tree to a <see cref="RegexNodeSpec"/> tree.</summary>
        private static RegexNodeSpec ConvertNode(RegexNode node, AnalysisResults analysis)
        {
            int childCount = node.ChildCount();
            ImmutableEquatableArray<RegexNodeSpec> children;
            if (childCount == 0)
            {
                children = ImmutableEquatableArray<RegexNodeSpec>.Empty;
            }
            else
            {
                RegexNodeSpec[] childSpecs = new RegexNodeSpec[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    childSpecs[i] = ConvertNode(node.Child(i), analysis);
                }

                children = new ImmutableEquatableArray<RegexNodeSpec>(childSpecs);
            }

            return new RegexNodeSpec(
                Kind: node.Kind,
                Options: node.Options,
                Ch: node.Ch,
                Str: node.Str,
                M: node.M,
                N: node.N,
                Children: children,
                IsAtomicByAncestor: analysis.IsAtomicByAncestor(node),
                MayBacktrack: analysis.MayBacktrack(node),
                MayContainCapture: analysis.MayContainCapture(node),
                IsInLoop: analysis.IsInLoop(node));
        }

        /// <summary>Converts <see cref="RegexFindOptimizations"/> to <see cref="FindOptimizationsSpec"/>.</summary>
        private static FindOptimizationsSpec ConvertFindOptimizations(RegexFindOptimizations opts, AnalysisResults analysis)
        {
            ImmutableEquatableArray<FixedDistanceSetSpec>? fixedDistanceSets = null;
            if (opts.FixedDistanceSets is { } sets)
            {
                fixedDistanceSets = sets.Select(s => new FixedDistanceSetSpec(
                    Set: s.Set,
                    Chars: s.Chars?.ToImmutableEquatableArray(),
                    Negated: s.Negated,
                    Distance: s.Distance,
                    Range: s.Range)).ToImmutableEquatableArray();
            }

            LiteralAfterLoopSpec? literalAfterLoop = null;
            if (opts.LiteralAfterLoop is { } lal)
            {
                literalAfterLoop = new LiteralAfterLoopSpec(
                    LoopNode: ConvertNode(lal.LoopNode, analysis),
                    LiteralChar: lal.Literal.Char,
                    LiteralString: lal.Literal.String,
                    LiteralStringComparison: lal.Literal.StringComparison,
                    LiteralChars: lal.Literal.Chars?.ToImmutableEquatableArray());
            }

            return new FindOptimizationsSpec(
                FindMode: opts.FindMode,
                LeadingAnchor: opts.LeadingAnchor,
                TrailingAnchor: opts.TrailingAnchor,
                MinRequiredLength: opts.MinRequiredLength,
                MaxPossibleLength: opts.MaxPossibleLength,
                LeadingPrefix: opts.LeadingPrefix,
                LeadingPrefixes: opts.LeadingPrefixes.ToImmutableEquatableArray(),
                FixedDistanceLiteral: opts.FixedDistanceLiteral,
                FixedDistanceSets: fixedDistanceSets,
                LiteralAfterLoop: literalAfterLoop);
        }
    }
}
