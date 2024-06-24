// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexEnumerateSplitTests
    {
        [Theory]
        [MemberData(nameof(RegexSplitTests.Split_TestData), MemberType = typeof(RegexSplitTests))]
        public async Task Split(RegexEngine engine, string pattern, string input, RegexOptions options, int count, int start, string[] _)
        {
            options |= RegexOptions.ExplicitCapture; // EnumerateSplits does not include the contents of capture groups, so avoid them when possible in the test patterns.

            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, start);
            bool isDefaultCount = RegexHelpers.IsDefaultCount(input, options, count);

            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);
            if (r.GetGroupNames().Length != 1)
            {
                // EnumerateSplits does not include the contents of capture groups.
                return;
            }

            if (isDefaultStart && isDefaultCount)
            {
                Validate(options, input, r.Split(input), r.EnumerateSplits(input));
                Validate(options, input, Regex.Split(input, pattern, options | RegexHelpers.OptionsFromEngine(engine)), Regex.EnumerateSplits(input, pattern, options | RegexHelpers.OptionsFromEngine(engine)));
            }

            if (isDefaultStart)
            {
                Validate(options, input, r.Split(input, count), r.EnumerateSplits(input, count));
            }

            Validate(options, input, r.Split(input, count, start), r.EnumerateSplits(input, count, start));

            static void Validate(RegexOptions options, string input, string[] expected, Regex.ValueSplitEnumerator enumerator)
            {
                var actual = new List<string>();
                while (enumerator.MoveNext())
                {
                    actual.Add(input[enumerator.Current]);
                }

                if ((options & RegexOptions.RightToLeft) != 0)
                {
                    actual.Reverse();
                }

                Assert.Equal(expected, actual.ToArray());
            }
        }

        [Fact]
        public void Split_Invalid()
        {
            // pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.EnumerateSplits("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.EnumerateSplits("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.EnumerateSplits("input", null, RegexOptions.None, TimeSpan.FromMilliseconds(1)));

            // count is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new Regex("pattern").EnumerateSplits("input", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new Regex("pattern").EnumerateSplits("input", -1, 0));

            // startat is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").EnumerateSplits("input", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").EnumerateSplits("input", 0, 6));
        }
    }
}
