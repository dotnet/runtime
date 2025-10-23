// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Tests;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexBalancingGroupTests
    {
        /// <summary>
        /// Tests for balancing groups where the balancing group's captured content
        /// precedes the position of the group being balanced.
        /// This tests the fix for https://github.com/dotnet/runtime/issues/XXXXX
        /// </summary>
        [Theory]
        [MemberData(nameof(BalancingGroup_WithConditional_MemberData))]
        public void BalancingGroup_WithConditional_ConsistentBehavior(RegexEngine engine, Regex regex, string input, bool expectedGroup2Matched, string expectedMatch)
        {
            _ = engine; // To satisfy xUnit analyzer
            Match m = regex.Match(input);

            Assert.True(m.Success, $"Match should succeed for input '{input}'");
            Assert.Equal(expectedMatch, m.Value);

            // Check that the group 2 state is consistent
            bool group2Success = m.Groups[2].Success;
            int group2CapturesCount = m.Groups[2].Captures.Count;

            // The key test: Group.Success and Captures.Count should be consistent with the conditional behavior
            Assert.Equal(expectedGroup2Matched, group2Success);
            if (expectedGroup2Matched)
            {
                Assert.True(group2CapturesCount > 0, "If group 2 matched, it should have at least one capture");
            }
            else
            {
                Assert.Equal(0, group2CapturesCount);
            }
        }

        public static IEnumerable<object[]> BalancingGroup_WithConditional_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    // NonBacktracking engine doesn't support balancing groups
                    continue;
                }

                var cases = new (string Pattern, string Input, bool ExpectedGroup2Matched, string ExpectedMatch)[]
                {
                    // Original bug report pattern
                    // The balancing group (?'2-1'(?'x1'..)) captures content that comes BEFORE group 1's capture
                    (@"\d+((?'x'[a-z-[b]]+)).(?<=(?'2-1'(?'x1'..)).{6})b(?(2)(?'Group2Captured'.)|(?'Group2NotCaptured'.))",
                        "00123xzacvb1", true, "00123xzacvb1"),

                    // Simpler test case: balancing group in forward context (normal case)
                    (@"(a)(?'2-1'b)(?(2)c|d)", "abc", true, "abc"),

                    // Balancing group in lookbehind where captured content comes after balanced group
                    (@"(a)b(?<=(?'2-1'.))c(?(2)d|e)", "abcd", true, "abcd"),

                    // Balancing group in lookbehind where captured content comes before balanced group (bug scenario)
                    (@"a(b)c(?<=(?'2-1'a)..)d(?(2)e|f)", "abcde", true, "abcde"),
                };

                Regex[] regexes = RegexHelpers.GetRegexes(engine, cases.Select(c => (c.Pattern, (System.Globalization.CultureInfo?)null, (RegexOptions?)null, (TimeSpan?)null)).ToArray());
                for (int i = 0; i < cases.Length; i++)
                {
                    yield return new object[] { engine, regexes[i], cases[i].Input, cases[i].ExpectedGroup2Matched, cases[i].ExpectedMatch };
                }
            }
        }

        /// <summary>
        /// Tests that IsMatched() behavior is consistent with Group.Success and Group.Captures.Count
        /// after TidyBalancing is called.
        /// </summary>
        [Theory]
        [MemberData(nameof(BalancingGroup_IsMatched_Consistency_MemberData))]
        public void BalancingGroup_IsMatched_Consistency(RegexEngine engine, Regex regex, string input, int groupNumber, bool expectedMatched)
        {
            _ = engine; // To satisfy xUnit analyzer
            Match m = regex.Match(input);

            Assert.True(m.Success, $"Match should succeed for input '{input}'");

            // Check that the group state is consistent
            bool groupSuccess = m.Groups[groupNumber].Success;
            int capturesCount = m.Groups[groupNumber].Captures.Count;

            Assert.Equal(expectedMatched, groupSuccess);
            if (expectedMatched)
            {
                Assert.True(capturesCount > 0, $"If group {groupNumber} matched, it should have at least one capture");
            }
            else
            {
                Assert.Equal(0, capturesCount);
            }
        }

        public static IEnumerable<object[]> BalancingGroup_IsMatched_Consistency_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    continue;
                }

                var cases = new (string Pattern, string Input, int GroupNumber, bool ExpectedMatched)[]
                {
                    // Group 1 should be balanced out (no captures remaining)
                    (@"(a)(?'2-1'b)", "ab", 1, false),

                    // Group 2 should have a capture
                    (@"(a)(?'2-1'b)", "ab", 2, true),

                    // Balancing in lookbehind - group 1 should be balanced out
                    (@"(a)b(?<=(?'2-1'.))c", "abc", 1, false),

                    // Balancing in lookbehind - group 2 should have a capture
                    (@"(a)b(?<=(?'2-1'.))c", "abc", 2, true),

                    // Original bug pattern - group 1 should be balanced out
                    (@"\d+((?'x'[a-z-[b]]+)).(?<=(?'2-1'(?'x1'..)).{6})b", "00123xzacvb", 1, false),

                    // Original bug pattern - group 2 should have a capture
                    (@"\d+((?'x'[a-z-[b]]+)).(?<=(?'2-1'(?'x1'..)).{6})b", "00123xzacvb", 2, true),
                };

                Regex[] regexes = RegexHelpers.GetRegexes(engine, cases.Select(c => (c.Pattern, (System.Globalization.CultureInfo?)null, (RegexOptions?)null, (TimeSpan?)null)).ToArray());
                for (int i = 0; i < cases.Length; i++)
                {
                    yield return new object[] { engine, regexes[i], cases[i].Input, cases[i].GroupNumber, cases[i].ExpectedMatched };
                }
            }
        }

        /// <summary>
        /// Tests various balancing group scenarios to ensure correct behavior.
        /// </summary>
        [Theory]
        [MemberData(nameof(BalancingGroup_Various_MemberData))]
        public void BalancingGroup_Various_Scenarios(RegexEngine engine, Regex regex, string input, string expectedValue, int expectedGroup1Count, int expectedGroup2Count)
        {
            _ = engine; // To satisfy xUnit analyzer
            Match m = regex.Match(input);

            Assert.True(m.Success);
            Assert.Equal(expectedValue, m.Value);
            Assert.Equal(expectedGroup1Count, m.Groups[1].Captures.Count);
            Assert.Equal(expectedGroup2Count, m.Groups[2].Captures.Count);
        }

        public static IEnumerable<object[]> BalancingGroup_Various_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    continue;
                }

                var cases = new (string Pattern, string Input, string ExpectedValue, int ExpectedGroup1Count, int ExpectedGroup2Count)[]
                {
                    // Basic balancing: group 1 captured, then balanced into group 2
                    // Creates a zero-length capture in group 2
                    (@"(a)(?'2-1'b)", "ab", "ab", 0, 1),

                    // Multiple captures: group 2 is the second (a), then balancing transfers from group 1
                    // Group 2 gets its own capture plus a zero-length capture from balancing
                    (@"(a)(a)(?'2-1'b)", "aab", "aab", 0, 2),
                };

                Regex[] regexes = RegexHelpers.GetRegexes(engine, cases.Select(c => (c.Pattern, (System.Globalization.CultureInfo?)null, (RegexOptions?)null, (TimeSpan?)null)).ToArray());
                for (int i = 0; i < cases.Length; i++)
                {
                    yield return new object[] { engine, regexes[i], cases[i].Input, cases[i].ExpectedValue, cases[i].ExpectedGroup1Count, cases[i].ExpectedGroup2Count };
                }
            }
        }
    }
}
