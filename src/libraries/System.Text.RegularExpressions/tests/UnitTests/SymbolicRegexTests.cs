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
        public void BDDCardinalityTests(UnicodeCategory category, uint expectedCardinality)
        {
            BDD digits = UnicodeCategoryConditions.GetCategory(category);
            Assert.Equal(expectedCardinality, ComputeCardinality(digits));

            // Returns how many characters this BDD represents
            static uint ComputeCardinality(BDD bdd)
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

            // all patterns are considered with RegexOptions.ExplicitCapture
            RegexOptions options = RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture;

            // pattern and its expected safe size
            // all patterns have an implicit 0-start-capture node ⌊₀ and
            // 0-end-capture node ⁰⌉ and thus also two extra cocatenation nodes
            // let the safe size of a pattern X be denoted by #(X)
            (string, int)[] patternData = new (string, int)[]{
                // no counters
                ("a", 5),                                  // #(a) + 4 = 5
                ("a|b", 5),                                // #[ab] + 1 + 4 = 5
                ("a*", 6),                                 // #(a) + 1 + 4 = 6
                ("a?", 6),
                ("ab", 7),
                ("a+", 8),                                 // #(a+) = #(aa*) = #(a)x2 + 2 + 4 = 8
                ("(abc)", 9),                              // #(abc) = 5 nodes and 4 concatenations
                // simple counters
                ("((ab){10})", 43),                        // #((ab){10}) = (#(ab) + 1)x10 + 3 = 4x10 + 3
                ("((ab){10,})", 48),                       // #((ab){10,}) = 4x10 + 3 (for a*) + 2 + 3 = 48
                ("((ab){0,10})", 53),                      // #((ab){0,10}) = (#(ab) + 2)x10 + 3, there are 10 ?-nodes also
                // nested counters
                ("(((ab){10}){10})", 403),                 // #(((ab){10}){10}) = ((#(ab) + 1)x10)x10 + 3 = 4x10x10 + 3
                ("(((ab){10}){0,10})", 413),               // 400 + 10 (for the ?-nodes) + 3
                ("(((ab){10}){10})|((cd){10})", 443),      // 400 + 40 + 3
                ("(((ab){10}){10})|((cd){0,10})", 453),    // 400 + 50 + 3
                ("(((ab){0,10}){10})|((cd){0,10})", 553),  // 500 + 50 + 3
            };

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
            int safeSize = ((SymbolicRegexNode<BDD>)node).EstimateSafeSize();
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
                "((ab){100,500})", 
                // nested small counters causing unsafe blowup through multiplicative nature of counter nesting
                "(((ab){10}){10}){10}",        // more than 10^3
                "((((ab){4}){4}){4}){4}",      // exponential: more than 4^5 = 1024
                // combined large counters
                "((ab){1000}){1000}",          // more than 1000^2
                "((ab){99999999}){99999999}",  // multiply: much more than int.MaxValue
                "(ab){0,1234567890}|(cd){1234567890,}" // sum: more than int.MaxValue
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
            int size = ((SymbolicRegexNode<BDD>)node).EstimateSafeSize();
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
