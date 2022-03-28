// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    // This test takes a long time to run since it needs to compute all possible lowercase mappings across
    // 3 different culutres and then creates Regex matches for all of our engines for each mapping.
    [OuterLoop]
    public class RegexIgnoreCaseTests
    {
        [Theory]
        [MemberData(nameof(Unicode_IgnoreCase_TestData))]
        public async Task Unicode_IgnoreCase_Tests(RegexEngine engine, string culture, RegexOptions options)
        {
            var testCases = GetPatternAndInputsForCulture(culture);

            foreach ((string pattern, string input) in testCases)
            {

                if ((options & RegexOptions.CultureInvariant) == 0)
                {
                    using var _ = new ThreadCultureChange(culture);
                    await ValidateMatch(pattern, input);
                    await ValidateMatch(input, pattern);
                }
                else
                {
                    await ValidateMatch(pattern, input);
                    await ValidateMatch(input, pattern);
                }
            }

            return;

            async Task ValidateMatch(string pattern, string input)
            {
                Regex regex = await RegexHelpers.GetRegexAsync(engine, pattern, options | RegexOptions.IgnoreCase);
                Assert.True(regex.IsMatch(input));
            }

            IEnumerable<(string, string)> GetPatternAndInputsForCulture(string culture)
            {
                TextInfo textInfo = string.IsNullOrEmpty(culture) ? CultureInfo.InvariantCulture.TextInfo
                    : CultureInfo.GetCultureInfo(culture).TextInfo;

                for (int i = 0; i < 0x1_0000; i++)
                {
                    char c = (char)i;
                    char lowerC = textInfo.ToLower(c);
                    if (c != lowerC)
                    {
                        yield return ($"{c}", $"{lowerC}");
                    }
                }
            }
        }

        public static IEnumerable<object[]> Unicode_IgnoreCase_TestData()
        {
            foreach (string culture in new[] { "", "en-US", "tr-TR" })
            {
                foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
                {
                    // Source generated engine does not support setting different culutres yet.
                    if (engine == RegexEngine.SourceGenerated && !string.IsNullOrEmpty(culture))
                        continue;

                    yield return new object[] { engine, culture, RegexOptions.None};
                    if (string.IsNullOrEmpty(culture))
                    {
                        // For the Invariant culture equivalences also test to get the same behavior with RegexOptions.CultureInvariant.
                        yield return new object[] { engine, culture, RegexOptions.CultureInvariant };
                    }
                }
            }
        }
    }
}
