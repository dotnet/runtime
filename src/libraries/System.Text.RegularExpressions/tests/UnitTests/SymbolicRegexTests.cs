// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;
using System.Text.RegularExpressions.Symbolic;
using System.Collections.Generic;

namespace System.Text.RegularExpressions.Tests
{
    public class SymbolicRegexTests
    {
        [Theory]
        [InlineData(UnicodeCategory.DecimalDigitNumber, 370)] //37 different kinds of decimal digits
        [InlineData(UnicodeCategory.Surrogate, 2048)]         //1024 low surrogates and 1024 high surrogates
        public void BDDNumberOfRepresentedCharactersTests(UnicodeCategory category, uint expectedCardinality)
        {
            BDD digits = UnicodeCategoryConditions.GetCategory(category);
            Assert.Equal(expectedCardinality, ComputeNumberOfRepresentedCharacters(digits));

            // Returns how many characters this BDD represents
            static uint ComputeNumberOfRepresentedCharacters(BDD bdd)
            {
                if (bdd.IsEmpty)
                {
                    return 0;
                }
                if (bdd.IsFull)
                {
                    return ushort.MaxValue;
                }

                (uint Lower, uint Upper)[] ranges = BDDRangeConverter.ToRanges(bdd);
                uint result = 0;
                for (int i = 0; i < ranges.Length; i++)
                {
                    result += ranges[i].Upper - ranges[i].Lower + 1;
                }
                return result;
            }
        }

        public static IEnumerable<object[]> SafeThresholdTests_MemberData()
        {
            var charSetSolver = new CharSetSolver();
            var bddBuilder = new SymbolicRegexBuilder<BDD>(charSetSolver, charSetSolver);
            var converter = new RegexNodeConverter(bddBuilder, null);

            RegexOptions options = RegexOptions.NonBacktracking;

            // pattern and its expected safe size
            // all patterns have an implicit 0-start-capture node ⌊₀ and
            // 0-end-capture node ⁰⌉ and thus also two extra cocatenation nodes
            // let the safe size of a pattern X be denoted by #(X)
            (string, int)[] patternData = new (string, int)[]{
                // no singletons
                ("()", 1),
                ("()*", 1),
                // no counters
                ("(a)", 2),
                ("(a|b)", 2),                               // (a|b) becomes [ab]
                ("(a*)", 2),
                ("(a?)", 2),
                ("(ab)", 3),
                ("(a+)", 3),                                // #(a+) = #(aa*) = 2 (1 is for the initial state)
                ("(abc)", 4),
                ("ab|c", 4),
                // simple counters
                ("(a{3,6})", 7),                            // 6x#(a) + 1
                ("((ab){10})", 21),
                ("((ab){10,})", 23),
                ("((ab){0,10})", 21),
                // nested counters
                ("(((ab){10}){10})", 201),
                ("(((ab){10}){0,10})", 201),
                ("(((ab){10}){10})|((cd){10})", 221),       // 200 + 20 + 1
                ("(((ab){10,}c){10})|((cd){9,})", 251),     // (2x11+1)x10 + 20 + 1
                ("(((ab){10,}c){10,})|((cd){0,10})", 274),  // (2x11+1)x11 + 20 + 1
                // lower bound int.MaxValue is never unfolded and treated as infinity
                ("(a{2147483647,})", 2),
                // typical case that blows up the DFA size to 2^100 when .* is added at the beginnig (below)
                ("a.{100}b", 103)
            };

            foreach ((string Pattern, int ExpectedSafeSize) in patternData)
            {
                RegexNode tree = RegexParser.Parse(Pattern, options | RegexOptions.ExplicitCapture, CultureInfo.CurrentCulture).Root;
                SymbolicRegexNode<BDD> rootNode = converter.ConvertToSymbolicRegexNode(tree);
                yield return new object[] { rootNode, ExpectedSafeSize };
            }

            // add .*? in front of the pattern, this adds 1 more NFA state
            foreach ((string Pattern, int ExpectedSafeSize) in patternData)
            {
                RegexNode tree = RegexParser.Parse(".*?" + Pattern, options | RegexOptions.ExplicitCapture, CultureInfo.CurrentCulture).Root;
                SymbolicRegexNode<BDD> rootNode = converter.ConvertToSymbolicRegexNode(tree);
                yield return new object[] { rootNode, 1 + ExpectedSafeSize};
            }

            // use of anchors increases the estimate by 5x in general but in reality much less, at most 3x
            foreach ((string Pattern, int ExpectedSafeSize) in patternData)
            {
                RegexNode tree = RegexParser.Parse(Pattern + "$", options | RegexOptions.ExplicitCapture, CultureInfo.CurrentCulture).Root;
                SymbolicRegexNode<BDD> rootNode = converter.ConvertToSymbolicRegexNode(tree);
                yield return new object[] { rootNode, 5 * ExpectedSafeSize };
            }

            // use of captures has no effect on the estimations
            foreach ((string Pattern, int ExpectedSafeSize) in patternData)
            {
                RegexNode tree = RegexParser.Parse(Pattern, options, CultureInfo.CurrentCulture).Root;
                SymbolicRegexNode<BDD> rootNode = converter.ConvertToSymbolicRegexNode(tree);
                yield return new object[] { rootNode, ExpectedSafeSize };
            }
        }

        [Theory]
        [MemberData(nameof(SafeThresholdTests_MemberData))]
        public void SafeThresholdTests(object obj, int expectedSafeSize)
        {
            SymbolicRegexNode<BDD> node = (SymbolicRegexNode<BDD>)obj;
            int safeSize = node.EstimateNfaSize();
            Assert.Equal(expectedSafeSize, safeSize);
            int nfaStateCount = CalculateNfaStateCount(node);
            Assert.True(nfaStateCount <= expectedSafeSize);
        }

        /// <summary>
        /// Compute the closure of all NFA states from root and return the size of the resulting state space.
        /// </summary>
        private static int CalculateNfaStateCount(SymbolicRegexNode<BDD> root)
        {
            // Here we are actually using the original BDD algebra (not converting to the BV or Uint64 algebra)
            // because it does not matter which algebra we use here (this matters only for performance)
            HashSet<(uint, SymbolicRegexNode<BDD>)> states = new();
            Stack<(uint, SymbolicRegexNode<BDD>)> frontier = new();
            List<BDD> minterms = MintermGenerator<BDD>.GenerateMinterms(root._builder._solver, root.GetSets());

            // Start from the initial state that has kind 'General' when no anchors are being used, else kind 'BeginningEnd'
            (uint, SymbolicRegexNode<BDD>) initialState = (root._info.ContainsSomeAnchor ? CharKind.BeginningEnd : CharKind.General, root);

            // Compute the closure of all NFA states from the given initial state
            states.Add(initialState);
            frontier.Push(initialState);
            while (frontier.Count > 0)
            {
                (uint Kind, SymbolicRegexNode<BDD> Node) source = frontier.Pop();

                // Iterate over all minterms to cover all possible inputs
                foreach (BDD minterm in minterms)
                {
                    uint kind = GetCharKind(minterm);
                    SymbolicRegexNode<BDD> target = source.Node.CreateDerivativeWithoutEffects(minterm, source.Kind);

                    //In the case of an NFA all the different alternatives in the DFA state become individual states themselves
                    foreach (SymbolicRegexNode<BDD> node in GetAlternatives(target))
                    {
                        (uint, SymbolicRegexNode<BDD>) state = (kind, node);
                        // Add the state to the set of states
                        if (states.Add(state))
                        {
                            // If state is new then it still needs to be explored
                            frontier.Push(state);
                        }
                    }
                }
            }

            return states.Count;

            // Enumerates the alternatives from a node, for eaxmple (ab|(bc|cd)) has three alternatives
            static IEnumerable<SymbolicRegexNode<BDD>> GetAlternatives(SymbolicRegexNode<BDD> node)
            {
                if (node._kind == SymbolicRegexNodeKind.Alternate)
                {
                    foreach (SymbolicRegexNode<BDD> elem in GetAlternatives(node._left!))
                        yield return elem;
                    foreach (SymbolicRegexNode<BDD> elem in GetAlternatives(node._right!))
                        yield return elem;
                }
                else if (!node.IsNothing) // omit deadend states
                {
                    yield return node;
                }
            }

            // Simplified character kind calculation that omits the special case that minterm can be the very last \n
            // This omission has practically no effect of the size of the state space, but would complicate the logic
            uint GetCharKind(BDD minterm) =>
                minterm.Equals(root._builder._newLineSet) ? CharKind.Newline :  // is \n
                (!root._builder._solver.IsEmpty(root._builder._solver.And(root._builder._wordLetterForBoundariesSet, minterm)) ?
                CharKind.WordLetter : // in \w
                CharKind.General);    // anything else, thus in particular in \W
        }

        public static IEnumerable<object[]> UnsafeThresholdTests_MemberData()
        {
            var charSetSolver = new CharSetSolver();
            var bddBuilder = new SymbolicRegexBuilder<BDD>(charSetSolver, charSetSolver);
            var converter = new RegexNodeConverter(bddBuilder, null);

            // all patterns are considered with RegexOptions.ExplicitCapture
            RegexOptions options = RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture;

            // patterns with large counters
            string[] patternData = new string[]{
                // simple counters that are too large
                "((ab){0,9000})",
                "((ab){1000})",
                "((ab){100,5000})",
                // almost infinite lower bound
                "a{2147483646,}",              // 2147483646 = int.MaxValue-1
                // nested small counters causing unsafe blowup through multiplicative nature of counter nesting
                "(((ab){10}){10}){10}",        // more than 10^3
                "((((abcd){4}){4}){4}){4}",    // exponential: more than 4^5 = 1024
                // combined large counters
                "((ab){1000}){1000}",          // more than 1000^2
                "((ab){99999999}){99999999}",  // multiply: much more than int.MaxValue
                "(ab){0,1234567890}|(cd){1234567890,}",// sum: more than int.MaxValue
            };

            foreach (string Pattern in patternData)
            {
                RegexNode tree = RegexParser.Parse(Pattern, options, CultureInfo.CurrentCulture).Root;
                SymbolicRegexNode<BDD> rootNode = converter.ConvertToSymbolicRegexNode(tree);
                yield return new object[] { rootNode };
            }
        }

        [Theory]
        [MemberData(nameof(UnsafeThresholdTests_MemberData))]
        public void UnsafeThresholdTests(object node)
        {
            int size = ((SymbolicRegexNode<BDD>)node).EstimateNfaSize();
            Assert.True(size > SymbolicRegexThresholds.GetSymbolicRegexSafeSizeThreshold());
        }

        [Theory]
        [InlineData(200, 200)]
        [InlineData(10_000, 10_000)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, SymbolicRegexThresholds.DefaultSymbolicRegexSafeSizeThreshold)]
        [InlineData(-47, SymbolicRegexThresholds.DefaultSymbolicRegexSafeSizeThreshold)]
        [InlineData("tmp", SymbolicRegexThresholds.DefaultSymbolicRegexSafeSizeThreshold)]
        [InlineData(null, SymbolicRegexThresholds.DefaultSymbolicRegexSafeSizeThreshold)]
        public void SafeThresholdConfigTest(object? newThresholdData, int expectedThreshold)
        {
            AppContext.SetData(SymbolicRegexThresholds.SymbolicRegexSafeSizeThreshold_ConfigKeyName, newThresholdData);
            int k = SymbolicRegexThresholds.GetSymbolicRegexSafeSizeThreshold();
            AppContext.SetData(SymbolicRegexThresholds.SymbolicRegexSafeSizeThreshold_ConfigKeyName, null);
            Assert.Equal(expectedThreshold, k);
        }
    }
}
