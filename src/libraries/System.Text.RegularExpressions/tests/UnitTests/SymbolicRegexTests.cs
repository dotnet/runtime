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
                ("(a)", 1),
                ("(a|b)", 1),                               // (a|b) becomes [ab]
                ("(a*)", 1),
                ("(a?)", 1),
                ("(ab)", 2),
                ("(a+)", 2),                                // #(a+) = #(aa*) = 2
                ("(abc)", 3),
                ("ab|c", 3),
                // simple counters
                ("((ab){10})", 20),
                ("((ab){10,})", 22),
                ("((ab){0,10})", 20),
                // nested counters
                ("(((ab){10}){10})", 200),
                ("(((ab){10}){0,10})", 200),
                ("(((ab){10}){10})|((cd){10})", 220),       // 200 + 20
                ("(((ab){10,}c){10})|((cd){9,})", 250),     // (2x11+1)x10 + 20
                ("(((ab){10,}c){10,})|((cd){0,10})", 273),  // (2x11+1)x11 + 20
                // lower bound int.MaxValue is never unfolded and treated as infinity
                ("(a{2147483647,})", 1),
            };

            foreach ((string Pattern, int ExpectedSafeSize) in patternData)
            {
                RegexNode tree = RegexParser.Parse(Pattern, options | RegexOptions.ExplicitCapture, CultureInfo.CurrentCulture).Root;
                SymbolicRegexNode<BDD> rootNode = converter.ConvertToSymbolicRegexNode(tree);
                yield return new object[] { rootNode, ExpectedSafeSize };
            }

            // use of anchors increases the estimate by 5x in general
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
        public void SafeThresholdTests(object node, int expectedSafeSize)
        {
            int safeSize = ((SymbolicRegexNode<BDD>)node).EstimateNfaSize();
            Assert.Equal(expectedSafeSize, safeSize);
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
