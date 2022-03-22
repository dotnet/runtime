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
        public async Task Unicode_IgnoreCase_Tests(RegexEngine engine, string culture, string pattern, string input, RegexOptions options)
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

            async Task ValidateMatch(string pattern, string input)
            {
                Regex regex = await RegexHelpers.GetRegexAsync(engine, pattern, options | RegexOptions.IgnoreCase);
                Assert.True(regex.IsMatch(input));
            }
        }

        public static IEnumerable<object[]> Unicode_IgnoreCase_TestData()
        {
            foreach ((string culture, char pattern, char input) in GetCaseEquivalencesPerCulture())
            {
                foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
                {
                    // Source generated engine does not support setting different culutres yet.
                    if (engine == RegexEngine.SourceGenerated && !string.IsNullOrEmpty(culture))
                        continue;

                    yield return new object[] { engine, culture, $"{pattern}", $"{input}", RegexOptions.None};
                    if (string.IsNullOrEmpty(culture))
                    {
                        // For the Invariant culture equivalences also test to get the same behavior with RegexOptions.CultureInvariant.
                        yield return new object[] { engine, culture, $"{pattern}", $"{input}", RegexOptions.CultureInvariant };
                    }
                }
            }
        }

        public static IEnumerable<(string, char, char)> GetCaseEquivalencesPerCulture()
        {
            foreach (string cultureName in new[] { "", "en-US", "tr-TR" })
            {
                TextInfo textInfo = string.IsNullOrEmpty(cultureName) ? CultureInfo.InvariantCulture.TextInfo
                    : CultureInfo.GetCultureInfo(cultureName).TextInfo;

                for (int i = 0; i < 0x1_0000; i++)
                {
                    char c = (char)i;
                    char lowerC = textInfo.ToLower(c);
                    if (c != lowerC)
                    {
                        yield return (cultureName, c, lowerC);
                    }
                }
            }
        }
    }
}
