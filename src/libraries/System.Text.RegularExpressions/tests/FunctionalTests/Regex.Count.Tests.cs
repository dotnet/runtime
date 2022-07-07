// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public partial class RegexCountTests
    {
        [Theory]
        [MemberData(nameof(Count_ReturnsExpectedCount_TestData))]
        public async Task Count_ReturnsExpectedCount(RegexEngine engine, string pattern, string input, int startat, RegexOptions options, int expectedCount)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);

            Assert.Equal(expectedCount, r.Count(input.AsSpan(), startat));
            Assert.Equal(r.Count(input.AsSpan(), startat), r.Matches(input, startat).Count);

            bool isDefaultStartAt = startat == ((options & RegexOptions.RightToLeft) != 0 ? input.Length : 0);
            if (!isDefaultStartAt)
            {
                return;
            }

            Assert.Equal(expectedCount, r.Count(input));
            Assert.Equal(expectedCount, r.Count(input.AsSpan()));
            Assert.Equal(r.Count(input), r.Matches(input).Count);
            Assert.Equal(r.Count(input.AsSpan()), r.Matches(input).Count);

            if (options == RegexOptions.None && engine == RegexEngine.Interpreter)
            {
                Assert.Equal(expectedCount, Regex.Count(input, pattern));
                Assert.Equal(expectedCount, Regex.Count(input.AsSpan(), pattern));
            }

            switch (engine)
            {
                case RegexEngine.Interpreter:
                case RegexEngine.Compiled:
                case RegexEngine.NonBacktracking:
                    RegexOptions engineOptions = RegexHelpers.OptionsFromEngine(engine);
                    Assert.Equal(expectedCount, Regex.Count(input, pattern, options | engineOptions));
                    Assert.Equal(expectedCount, Regex.Count(input.AsSpan(), pattern, options | engineOptions));
                    Assert.Equal(expectedCount, Regex.Count(input, pattern, options | engineOptions, Regex.InfiniteMatchTimeout));
                    Assert.Equal(expectedCount, Regex.Count(input.AsSpan(), pattern, options | engineOptions, Regex.InfiniteMatchTimeout));
                    break;
            }
        }

        public static IEnumerable<object[]> Count_ReturnsExpectedCount_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, @"", "", 0, RegexOptions.None, 1 };
                yield return new object[] { engine, @"", "a", 0, RegexOptions.None, 2 };
                yield return new object[] { engine, @"", "ab", 0, RegexOptions.None, 3 };
                yield return new object[] { engine, @"", "ab", 1, RegexOptions.None, 2 };

                yield return new object[] { engine, @"\w", "", 0, RegexOptions.None, 0 };
                yield return new object[] { engine, @"\w", "a", 0, RegexOptions.None, 1 };
                yield return new object[] { engine, @"\w", "ab", 0, RegexOptions.None, 2 };
                yield return new object[] { engine, @"\w", "ab", 1, RegexOptions.None, 1 };
                yield return new object[] { engine, @"\w", "ab", 2, RegexOptions.None, 0 };

                yield return new object[] { engine, @"\b\w+\b", "abc def ghi jkl", 0, RegexOptions.None, 4 };
                yield return new object[] { engine, @"\b\w+\b", "abc def ghi jkl", 7, RegexOptions.None, 2 };

                yield return new object[] { engine, @"A", "", 0, RegexOptions.IgnoreCase, 0 };
                yield return new object[] { engine, @"A", "a", 0, RegexOptions.IgnoreCase, 1 };
                yield return new object[] { engine, @"A", "aAaA", 0, RegexOptions.IgnoreCase, 4 };

                yield return new object[] { engine, @".", "\n\n\n", 0, RegexOptions.None, 0 };
                yield return new object[] { engine, @".", "\n\n\n", 0, RegexOptions.Singleline, 3 };

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    // Lookbehinds
                    yield return new object[] { engine, @"(?<=abc)\w", "abcxabcy", 7, RegexOptions.None, 1 };

                    // Starting anchors
                    yield return new object[] { engine, @"\Gdef", "abcdef", 0, RegexOptions.None, 0 };
                    yield return new object[] { engine, @"\Gdef", "abcdef", 3, RegexOptions.None, 1 };

                    // RightToLeft
                    yield return new object[] { engine, @"\b\w+\b", "abc def ghi jkl", 15, RegexOptions.RightToLeft, 4 };
                    yield return new object[] { RegexEngine.Interpreter, @"(?<=abc)\w", "abcxabcy", 8, RegexOptions.RightToLeft, 2 };
                    yield return new object[] { engine, @"(?<=abc)\w", "abcxabcy", 7, RegexOptions.RightToLeft, 1 };
                }
            }
        }

        [Fact]
        public void Count_InvalidArguments_Throws()
        {
            // input is null
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Count(null));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Count(null, @"pattern"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Count(null, @"pattern", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Count(null, @"pattern", RegexOptions.None, TimeSpan.FromMilliseconds(1)));

            // pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Count("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Count("input".AsSpan(), null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Count("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Count("input".AsSpan(), null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Count("input", null, RegexOptions.None, TimeSpan.FromMilliseconds(1)));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Count("input".AsSpan(), null, RegexOptions.None, TimeSpan.FromMilliseconds(1)));

            // pattern is invalid
#pragma warning disable RE0001 // invalid regex pattern
            AssertExtensions.Throws<RegexParseException>(() => Regex.Count("input", @"[abc"));
            AssertExtensions.Throws<RegexParseException>(() => Regex.Count("input".AsSpan(), @"[abc"));
            AssertExtensions.Throws<RegexParseException>(() => Regex.Count("input", @"[abc", RegexOptions.None));
            AssertExtensions.Throws<RegexParseException>(() => Regex.Count("input".AsSpan(), @"[abc", RegexOptions.None));
            AssertExtensions.Throws<RegexParseException>(() => Regex.Count("input", @"[abc", RegexOptions.None, TimeSpan.FromMilliseconds(1)));
            AssertExtensions.Throws<RegexParseException>(() => Regex.Count("input".AsSpan(), @"[abc", RegexOptions.None, TimeSpan.FromMilliseconds(1)));
#pragma warning restore RE0001

            // options is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Count("input", @"[abc]", (RegexOptions)(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Count("input".AsSpan(), @"[abc]", (RegexOptions)(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Count("input", @"[abc]", (RegexOptions)(-1), TimeSpan.FromMilliseconds(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Count("input".AsSpan(), @"[abc]", (RegexOptions)(-1), TimeSpan.FromMilliseconds(1)));

            // matchTimeout is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => Regex.Count("input", @"[abc]", RegexOptions.None, TimeSpan.FromMilliseconds(-2)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => Regex.Count("input".AsSpan(), @"[abc]", RegexOptions.None, TimeSpan.FromMilliseconds(-2)));
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Count_Timeout_ThrowsAfterTooLongExecution(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // Test relies on backtracking taking a long time
                return;
            }

            const string Pattern = @"^(\w+\s?)*$";
            const string Input = "An input string that takes a very very very very very very very very very very very long time!";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(1));

            Stopwatch sw = Stopwatch.StartNew();
            Assert.Throws<RegexMatchTimeoutException>(() => r.Count(Input));
            Assert.Throws<RegexMatchTimeoutException>(() => r.Count(Input.AsSpan()));
            Assert.InRange(sw.Elapsed.TotalSeconds, 0, 30); // arbitrary upper bound that should be well above what's needed with a 1ms timeout

            switch (engine)
            {
                case RegexEngine.Interpreter:
                case RegexEngine.Compiled:
                    sw = Stopwatch.StartNew();
                    Assert.Throws<RegexMatchTimeoutException>(() => Regex.Count(Input, Pattern, RegexHelpers.OptionsFromEngine(engine), TimeSpan.FromMilliseconds(1)));
                    Assert.Throws<RegexMatchTimeoutException>(() => Regex.Count(Input.AsSpan(), Pattern, RegexHelpers.OptionsFromEngine(engine), TimeSpan.FromMilliseconds(1)));
                    Assert.InRange(sw.Elapsed.TotalSeconds, 0, 30); // arbitrary upper bound that should be well above what's needed with a 1ms timeout
                    break;
            }
        }
    }
}
