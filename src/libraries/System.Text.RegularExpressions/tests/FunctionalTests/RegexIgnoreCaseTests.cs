// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexIgnoreCaseTests
    {
        public static IEnumerable<(string, string)> CharactersWithSameLowercase()
        {
            return new (string, string)[]
            {
                new("\u0130", "\u0049"), // Both lowercase to \u0069
                new("\u01C5", "\u01C4"), // Both lowercase to \u01C6
                new("\u01C8", "\u01C7"), // Both lowercase to \u01C9
                new("\u01CB", "\u01CA"), // Both lowercase to \u01CC
                new("\u01F2", "\u01F1"), // Both lowercase to \u01F3
                new("\u03F4", "\u0398"), // Both lowercase to \u03B8
                new("\u2126", "\u03A9"), // Both lowercase to \u03C9
                new("\u212A", "\u004B"), // Both lowercase to \u006B
                new("\u212B", "\u00C5"), // Both lowercase to \u00E5
            };
        }

        public static IEnumerable<object[]> Characters_With_Common_Lowercase_Match_Data()
        {
            foreach ((string pattern, string input) in CharactersWithSameLowercase())
            {
                foreach(RegexEngine engine in RegexHelpers.AvailableEngines)
                {
                    yield return new object[] { engine, pattern, input };
                }
            }
        }

        public static IEnumerable<object[]> Characters_With_Common_Lowercase_Match_Backreference_Data()
        {
            foreach ((string firstChar, string secondChar) in CharactersWithSameLowercase())
            {
                foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
                {
                    if (engine == RegexEngine.NonBacktracking) // Backreferences are not yet supported by the NonBacktracking engine
                        continue;

                    yield return new object[] { engine, /*lang=regex*/@"(.)\1", firstChar, secondChar };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Characters_With_Common_Lowercase_Match_Data))]
        public async Task Characters_With_Common_Lowercase_Match(RegexEngine engine, string pattern, string input)
        {
            Regex regex = await RegexHelpers.GetRegexAsync(engine, pattern, RegexOptions.IgnoreCase);
            Assert.True(regex.IsMatch(input));
        }

        public static IEnumerable<object[]> EnginesThatSupportBackreferences()
        {
            foreach(var engine in RegexHelpers.AvailableEngines)
            {
                if (engine == RegexEngine.NonBacktracking) // Nonbacktracking engine doesn't yet support backreferences.
                    continue;
                yield return new object[] { engine };
            }
        }

        [Theory]
        [MemberData(nameof(EnginesThatSupportBackreferences))]
        public async Task IgnoreCase_Behavior_Is_Constant(RegexEngine engine)
        {
            Regex regex;
            using (_ = new ThreadCultureChange("en-US"))
            {
                regex = await RegexHelpers.GetRegexAsync(engine, @"(i)\1", RegexOptions.IgnoreCase);
            }

            using (_ = new ThreadCultureChange("tr-TR"))
            {
                // tr-TR culture doesn't consider 'I' and 'i' to be equal in ignore case, but en-US culture does.
                // This test will validate that the backreference will use en-US culture even when current culture is
                // set to tr-TR
                Assert.True(regex.IsMatch("Ii"));
            }
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser uses Invariant mode which causes invariant mappings to be used and will fail to match '\u0130' and 'I'")]
        [MemberData(nameof(Characters_With_Common_Lowercase_Match_Backreference_Data))]
        public async Task Characters_With_Common_Lowercase_Match_Backreference(RegexEngine engine, string pattern, string firstChar, string secondChar)
        {
            Regex regex = await RegexHelpers.GetRegexAsync(engine, pattern, RegexOptions.IgnoreCase);
            Assert.True(regex.IsMatch($"{firstChar}{secondChar}"));
            Assert.True(regex.IsMatch($"{secondChar}{firstChar}"));
        }

        [Theory]
        [MemberData(nameof(EnginesThatSupportBackreferences))]
        public async Task Ensure_CultureInvariant_Option_Is_Used_For_Backreferences(RegexEngine engine)
        {
            using (_ = new ThreadCultureChange("tr-TR"))
            {
                Regex regex = await RegexHelpers.GetRegexAsync(engine, @"(.)\1", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                // There is no mapping between 'i' and 'I' in tr-TR culture, so this test is validating that when passing CultureInvariant
                // option, we will use InvariantCulture mappings for backreferences.
                Assert.True(regex.IsMatch("iI"));
            }
        }

        [Fact]
        // This test creates a source generated engine for each of the ~870 cultures and ensures the result compiles. This test alone takes around 30
        // seconds on a fast machine, so marking as OuterLoop.
        [OuterLoop]
        public async Task SourceGenerator_Supports_All_Cultures()
        {
            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                using (_ = new ThreadCultureChange(culture))
                {
                    // This test will try to emit code that looks like: textInfo = CultureInfo.GetCultureInfo(CurrentCulture.Name).TextInfo
                    // so we will validate in this test that we are able to do that for all cultures and that GetCultureInfo returns a valid Culture.
                    _ = await RegexHelpers.GetRegexAsync(RegexEngine.SourceGenerated, @"(.)\1", RegexOptions.IgnoreCase);
                }
            }
        }

        // This test takes a long time to run since it needs to compute all possible lowercase mappings across
        // 3 different cultures and then creates Regex matches for all of our engines for each mapping.
        [OuterLoop]
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
            foreach (string culture in new[] { "", "EN-US", "tr-TR", "AZ" })
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
