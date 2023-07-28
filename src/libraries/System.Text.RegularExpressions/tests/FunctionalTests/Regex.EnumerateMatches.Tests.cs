// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public void EnumerateMatches_Lookahead(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookaheads not supported
                return;
            }

            const string Pattern = @"\b(?!un)\w+\b";
            const string Input = "unite one unethical ethics use untie ultimate";

            Regex r = RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase).GetAwaiter().GetResult();
            int count = 0;
            string[] expectedMatches = new[] { "one", "ethics", "use", "ultimate" };
            ReadOnlySpan<char> span = Input.AsSpan();
            foreach (ValueMatch match in r.EnumerateMatches(span))
            {
                Assert.Equal(expectedMatches[count++], span.Slice(match.Index, match.Length).ToString());
            }
            Assert.Equal(4, count);
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public void EnumerateMatches_Lookbehind(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookbehinds not supported
                return;
            }

            const string Pattern = @"(?<=\b20)\d{2}\b";
            const string Input = "2010 1999 1861 2140 2009";

            Regex r = RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase).GetAwaiter().GetResult();
            int count = 0;
            string[] expectedMatches = new[] { "10", "09" };
            ReadOnlySpan<char> span = Input.AsSpan();
            foreach (ValueMatch match in r.EnumerateMatches(span))
            {
                Assert.Equal(expectedMatches[count++], span.Slice(match.Index, match.Length).ToString());
            }
            Assert.Equal(2, count);
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public void EnumerateMatches_CheckIndex(RegexEngine engine)
        {
            const string Pattern = @"e{2}\w\b";
            const string Input = "needing a reed";

            Regex r = RegexHelpers.GetRegexAsync(engine, Pattern).GetAwaiter().GetResult();
            int count = 0;
            string[] expectedMatches = new[] { "eed" };
            int[] expectedIndex = new[] { 11 };
            ReadOnlySpan<char> span = Input.AsSpan();
            foreach (ValueMatch match in r.EnumerateMatches(span))
            {
                Assert.Equal(expectedMatches[count], span.Slice(match.Index, match.Length).ToString());
                Assert.Equal(expectedIndex[count++], match.Index);
            }
        }
    }

    public partial class RegexMultipleMatchTests
    {
        [Theory]
        [MemberData(nameof(Matches_TestData))]
        public void EnumerateMatches(RegexEngine engine, string pattern, string input, RegexOptions options, CaptureData[] expected)
        {
            Regex regexAdvanced = RegexHelpers.GetRegexAsync(engine, pattern, options).GetAwaiter().GetResult();
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

    public partial class RegexMatchTests
    {
        [Theory]
        [MemberData(nameof(Match_Count_TestData))]
        public void EnumerateMatches_Count(RegexEngine engine, string pattern, string input, int expectedCount)
        {
            Regex r = RegexHelpers.GetRegexAsync(engine, pattern).GetAwaiter().GetResult();
            int count = 0;
            foreach (ValueMatch _ in r.EnumerateMatches(input))
            {
                count++;
            }
            Assert.Equal(expectedCount, count);
        }
    }

    public partial class RegexCountTests
    {
        [Theory]
        [MemberData(nameof(Count_ReturnsExpectedCount_TestData))]
        public void EnumerateMatches_ReturnsExpectedCount(RegexEngine engine, string pattern, string input, int startat, RegexOptions options, int expectedCount)
        {
            Regex r = RegexHelpers.GetRegexAsync(engine, pattern, options).GetAwaiter().GetResult();

            int count;

            count = 0;
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
