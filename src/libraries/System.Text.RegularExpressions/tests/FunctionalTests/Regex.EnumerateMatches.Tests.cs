// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexEnumerateMatchesTests
    {
        public static IEnumerable<object[]> NoneCompiledBacktracking()
        {
            yield return new object[] { RegexOptions.None };
            yield return new object[] { RegexOptions.Compiled };
            if (PlatformDetection.IsNetCore)
            {
                yield return new object[] { RegexHelpers.RegexOptionNonBacktracking };
            }
        }

        [Fact]
        public void EnumerateMatches_Ctor_Invalid()
        {
            // Pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.EnumerateMatches("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.EnumerateMatches("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.EnumerateMatches("input", null, RegexOptions.None, TimeSpan.FromSeconds(1)));

            // Options are invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.EnumerateMatches("input", "pattern", (RegexOptions)(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.EnumerateMatches("input", "pattern", (RegexOptions)(-1), TimeSpan.FromSeconds(1)));

            // 0x400 is new NonBacktracking mode that is now valid, 0x800 is still invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.EnumerateMatches("input", "pattern", (RegexOptions)0x800));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.EnumerateMatches("input", "pattern", (RegexOptions)0x800, TimeSpan.FromSeconds(1)));

            // MatchTimeout is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => Regex.EnumerateMatches("input", "pattern", RegexOptions.None, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => Regex.EnumerateMatches("input", "pattern", RegexOptions.None, TimeSpan.Zero));
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task EnumerateMatches_Lookahead(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookaheads not supported
                return;
            }

            Test("unite one unethical ethics use untie ultimate",
                 await RegexHelpers.GetRegexAsync(engine, @"\b(?!un)\w+\b", RegexOptions.IgnoreCase),
                 ["one", "ethics", "use", "ultimate"]);

            static void Test(string input, Regex r, string[] expectedMatches)
            {
                ReadOnlySpan<char> span = input.AsSpan();

                int count = 0;
                foreach (ValueMatch match in r.EnumerateMatches(span))
                {
                    Assert.Equal(expectedMatches[count++], span.Slice(match.Index, match.Length).ToString());
                }

                Assert.Equal(expectedMatches.Length, count);
            }
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task EnumerateMatches_Lookbehind(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookbehinds not supported
                return;
            }

            Test("2010 1999 1861 2140 2009",
                 await RegexHelpers.GetRegexAsync(engine, @"(?<=\b20)\d{2}\b", RegexOptions.IgnoreCase),
                 ["10", "09"]);

            static void Test(string input, Regex r, string[] expectedMatches)
            {
                ReadOnlySpan<char> span = input.AsSpan();

                int count = 0;
                foreach (ValueMatch match in r.EnumerateMatches(span))
                {
                    Assert.Equal(expectedMatches[count++], span.Slice(match.Index, match.Length).ToString());
                }

                Assert.Equal(expectedMatches.Length, count);
            }
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task EnumerateMatches_CheckIndex(RegexEngine engine)
        {
            Test("needing a reed",
                 await RegexHelpers.GetRegexAsync(engine, @"e{2}\w\b"),
                 ["eed"],
                 [11]);

            static void Test(string input, Regex r, string[] expectedMatches, int[] expectedIndex)
            {
                ReadOnlySpan<char> span = input.AsSpan();

                int count = 0;
                foreach (ValueMatch match in r.EnumerateMatches(span))
                {
                    Assert.Equal(expectedMatches[count], span.Slice(match.Index, match.Length).ToString());
                    Assert.Equal(expectedIndex[count], match.Index);

                    count++;
                }
            }
        }
    }

    public partial class RegexMultipleMatchTests
    {
        [Theory]
        [MemberData(nameof(Matches_TestData))]
        public async Task EnumerateMatches(RegexEngine engine, string pattern, string input, RegexOptions options, CaptureData[] expected)
        {
            Test(input, expected, await RegexHelpers.GetRegexAsync(engine, pattern, options));

            static void Test(string input, CaptureData[] expected, Regex regexAdvanced)
            {
                int count = 0;
                ReadOnlySpan<char> span = input.AsSpan();
                foreach (ValueMatch match in regexAdvanced.EnumerateMatches(span))
                {
                    Assert.Equal(expected[count].Index, match.Index);
                    Assert.Equal(expected[count].Length, match.Length);
                    Assert.Equal(expected[count].Value, span.Slice(match.Index, match.Length).ToString());
                    count++;
                }

                Assert.Equal(expected.Length, count);
            }
        }
    }

    public partial class RegexMatchTests
    {
        [Theory]
        [MemberData(nameof(Match_Count_TestData))]
        public async Task EnumerateMatches_Count(RegexEngine engine, string pattern, string input, int expectedCount)
        {
            Test(input, expectedCount, await RegexHelpers.GetRegexAsync(engine, pattern));

            static void Test(string input, int expectedCount, Regex r)
            {
                int count = 0;
                foreach (ValueMatch _ in r.EnumerateMatches(input))
                {
                    count++;
                }

                Assert.Equal(expectedCount, count);
            }
        }
    }

    public partial class RegexCountTests
    {
        [Theory]
        [MemberData(nameof(Count_ReturnsExpectedCount_TestData))]
        public async Task EnumerateMatches_ReturnsExpectedCount(RegexEngine engine, string pattern, string input, int startat, RegexOptions options, int expectedCount)
        {
            Test(engine, pattern, input, startat, options, expectedCount, await RegexHelpers.GetRegexAsync(engine, pattern, options));

            static void Test(RegexEngine engine, string pattern, string input, int startat, RegexOptions options, int expectedCount, Regex r)
            {
                int count = 0;
                foreach (ValueMatch _ in r.EnumerateMatches(input, startat))
                {
                    count++;
                }
                Assert.Equal(expectedCount, count);

                bool isDefaultStartAt = startat == ((options & RegexOptions.RightToLeft) != 0 ? input.Length : 0);
                if (!isDefaultStartAt)
                {
                    return;
                }

                if (options == RegexOptions.None && engine == RegexEngine.Interpreter)
                {
                    count = 0;
                    foreach (ValueMatch _ in Regex.EnumerateMatches(input, pattern))
                    {
                        count++;
                    }
                    Assert.Equal(expectedCount, count);
                }

                switch (engine)
                {
                    case RegexEngine.Interpreter:
                    case RegexEngine.Compiled:
                    case RegexEngine.NonBacktracking:
                        RegexOptions engineOptions = RegexHelpers.OptionsFromEngine(engine);
                        count = 0;
                        foreach (ValueMatch _ in Regex.EnumerateMatches(input, pattern, options | engineOptions))
                        {
                            count++;
                        }
                        Assert.Equal(expectedCount, count);

                        count = 0;
                        foreach (ValueMatch _ in Regex.EnumerateMatches(input, pattern, options | engineOptions, Regex.InfiniteMatchTimeout))
                        {
                            count++;
                        }
                        Assert.Equal(expectedCount, count);
                        break;
                }
            }
        }
    }
}
