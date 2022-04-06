// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexCultureTests
    {
        public static IEnumerable<object[]> CharactersComparedOneByOne_AnchoredPattern_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "^aa$", "aA", "da-DK", RegexOptions.None, false };
                yield return new object[] { engine, "^aA$", "aA", "da-DK", RegexOptions.None, true };
                yield return new object[] { engine, "^aa$", "aA", "da-DK", RegexOptions.IgnoreCase, true };
                yield return new object[] { engine, "^aA$", "aA", "da-DK", RegexOptions.IgnoreCase, true };
            }
        }

        [Theory]
        [MemberData(nameof(CharactersComparedOneByOne_AnchoredPattern_TestData))]
        public async Task CharactersComparedOneByOne_AnchoredPattern(RegexEngine engine, string pattern, string input, string culture, RegexOptions options, bool expected)
        {
            // Regex compares characters one by one.  If that changes, it could impact the behavior of
            // a case like this, where these characters are not the same, but the strings compare
            // as equal with the invariant culture (and some other cultures as well).
            using (new ThreadCultureChange(culture))
            {
                Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);
                Assert.Equal(expected, r.IsMatch(input));
            }
        }

        public static IEnumerable<object[]> CharactersComparedOneByOne_Invariant_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, RegexOptions.None };
                yield return new object[] { engine, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant };
            }
        }

        [Theory]
        [MemberData(nameof(CharactersComparedOneByOne_Invariant_TestData))]
        public async Task CharactersComparedOneByOne_Invariant(RegexEngine engine, RegexOptions options)
        {
            // Regex compares characters one by one.  If that changes, it could impact the behavior of
            // a case like this, where these characters are not the same, but the strings compare
            // as equal with the invariant culture (and some other cultures as well).
            const string S1 = "\u00D6\u200D";
            const string S2 = "\u004F\u0308";

            // Validate the chosen strings to make sure they compare the way we want to test via Regex
            Assert.False(S1[0] == S2[0]);
            Assert.False(S1[1] == S2[1]);
            Assert.StartsWith(S1, S2, StringComparison.InvariantCulture);
            Assert.True(S1.Equals(S2, StringComparison.InvariantCulture));

            // Test varying lengths of strings to validate codegen changes that kick in at longer lengths
            foreach (int multiple in new[] { 1, 10, 100 })
            {
                string pattern = string.Concat(Enumerable.Repeat(S1, multiple));
                string input = string.Concat(Enumerable.Repeat(S2, multiple));
                Regex r;

                // Validate when the string is at the beginning of the pattern, as it impacts prefix matching.
                r = await RegexHelpers.GetRegexAsync(engine, pattern, options);
                Assert.False(r.IsMatch(input));
                Assert.True(r.IsMatch(pattern));

                // Validate when it's not at the beginning of the pattern, as it impacts "multi" matching.
                r = await RegexHelpers.GetRegexAsync(engine, "[abc]" + pattern, options);
                Assert.False(r.IsMatch("a" + input));
                Assert.True(r.IsMatch("a" + pattern));
            }
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task CharactersLowercasedOneByOne(RegexEngine engine)
        {
            using (new ThreadCultureChange("en-US"))
            {
                Assert.True((await RegexHelpers.GetRegexAsync(engine, "\uD801\uDC00", RegexOptions.IgnoreCase)).IsMatch("\uD801\uDC00"));
                Assert.True((await RegexHelpers.GetRegexAsync(engine, "\uD801\uDC00", RegexOptions.IgnoreCase)).IsMatch("abcdefg\uD801\uDC00"));
                Assert.True((await RegexHelpers.GetRegexAsync(engine, "\uD801", RegexOptions.IgnoreCase)).IsMatch("\uD801\uDC00"));
                Assert.True((await RegexHelpers.GetRegexAsync(engine, "\uDC00", RegexOptions.IgnoreCase)).IsMatch("\uD801\uDC00"));
            }
        }

        public static IEnumerable<object[]> TurkishI_Is_Differently_LowerUpperCased_In_Turkish_Culture_TestData()
        {
            // this test fails for NonBacktracking, see next test
            yield return new object[] { 2, RegexOptions.None };
            yield return new object[] { 256, RegexOptions.None };
        }

        /// <summary>
        /// See https://en.wikipedia.org/wiki/Dotted_and_dotless_I
        /// </summary>
        [Theory]
        [MemberData(nameof(TurkishI_Is_Differently_LowerUpperCased_In_Turkish_Culture_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56407", TestPlatforms.Android)]
        public void TurkishI_Is_Differently_LowerUpperCased_In_Turkish_Culture(int length, RegexOptions options)
        {
            var turkish = new CultureInfo("tr-TR");
            string input = string.Concat(Enumerable.Repeat("I\u0131\u0130i", length / 2));

            Regex[] cultInvariantRegex = Create(input, CultureInfo.InvariantCulture, options | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            Regex[] turkishRegex = Create(input, turkish, options | RegexOptions.IgnoreCase);

            // same input and regex does match so far so good
            Assert.All(cultInvariantRegex, rex => Assert.True(rex.IsMatch(input)));
            if (PlatformDetection.IsNetFramework)
            {
                // If running in .NET Framework, when the Regex was created with a turkish locale the lower cased turkish version will
                // no longer match the input string which contains upper and lower case iiiis hence even the input string
                // will no longer match. For more info, check https://github.com/dotnet/runtime/issues/58958
                Assert.All(turkishRegex, rex => Assert.False(rex.IsMatch(input)));
            }
            else
            {
                Assert.All(turkishRegex, rex => Assert.True(rex.IsMatch(input)));
            }

            // Now comes the tricky part depending on the use locale in ToUpper the results differ
            // Hence the regular expression will not match if different locales were used
            Assert.All(cultInvariantRegex, rex => Assert.True(rex.IsMatch(input.ToLowerInvariant())));
            Assert.All(cultInvariantRegex, rex => Assert.False(rex.IsMatch(input.ToLower(turkish))));

            Assert.All(turkishRegex, rex => Assert.False(rex.IsMatch(input.ToLowerInvariant())));
            Assert.All(turkishRegex, rex => Assert.True(rex.IsMatch(input.ToLower(turkish))));
        }

        /// <summary>
        /// Create regular expression once compiled and once interpreted to check if both behave the same
        /// </summary>
        /// <param name="input">Input regex string</param>
        /// <param name="info">thread culture to use when creating the regex</param>
        /// <param name="additional">Additional regex options</param>
        /// <returns></returns>
        Regex[] Create(string input, CultureInfo info, RegexOptions additional)
        {
            using (new ThreadCultureChange(info))
            {
                // When RegexOptions.IgnoreCase is supplied the current thread culture is used to lowercase the input string.
                // Except if RegexOptions.CultureInvariant is additionally added locale dependent effects on the generated code or state machine may happen.
                return new Regex[]
                {
                    new Regex(input, additional),
                    new Regex(input, RegexOptions.Compiled | additional)
                };
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't support NonBacktracking")]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60568", TestPlatforms.Android)]
        public async Task TurkishI_Is_Differently_LowerUpperCased_In_Turkish_Culture_NonBacktracking()
        {
            var turkish = new CultureInfo("tr-TR");
            string input = "I\u0131\u0130i";

            // Use the input as the regex also
            // Ignore the Compiled option here because it is a noop in combination with NonBacktracking 
            Regex cultInvariantRegex = await RegexHelpers.GetRegexAsync(RegexEngine.NonBacktracking, input, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, CultureInfo.InvariantCulture);
            Regex turkishRegex = await RegexHelpers.GetRegexAsync(RegexEngine.NonBacktracking, input, RegexOptions.IgnoreCase, turkish);

            Assert.True(cultInvariantRegex.IsMatch(input));
            Assert.True(turkishRegex.IsMatch(input));    // <---------- This result differs from the result in the previous test!!!

            // As above and no surprises here
            // The regexes recognize different lowercase variants of different versions of i differently
            Assert.True(cultInvariantRegex.IsMatch(input.ToLowerInvariant()));
            Assert.False(cultInvariantRegex.IsMatch(input.ToLower(turkish)));

            Assert.False(turkishRegex.IsMatch(input.ToLowerInvariant()));
            Assert.True(turkishRegex.IsMatch(input.ToLower(turkish)));

            // The same holds symmetrically for ToUpper
            Assert.True(cultInvariantRegex.IsMatch(input.ToUpperInvariant()));
            Assert.False(cultInvariantRegex.IsMatch(input.ToUpper(turkish)));

            Assert.False(turkishRegex.IsMatch(input.ToUpperInvariant()));
            Assert.True(turkishRegex.IsMatch(input.ToUpper(turkish)));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/58958")]
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task TurkishCulture_Handling_Of_IgnoreCase(RegexEngine engine)
        {
            var turkish = new CultureInfo("tr-TR");
            string input = "I\u0131\u0130i";
            string pattern = "[H-J][\u0131-\u0140][\u0120-\u0130][h-j]";

            Regex regex = await RegexHelpers.GetRegexAsync(engine, pattern, RegexOptions.IgnoreCase, turkish);

            // The pattern must trivially match the input because all of the letters fall in the given intervals
            // Ignoring case can only add more letters here -- not REMOVE letters
            Assert.True(regex.IsMatch(input));
        }

        public static IEnumerable<object[]> TurkishCulture_MatchesWordChar_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "I\u0131\u0130i", RegexOptions.None, "I\u0131\u0130i" };
                yield return new object[] { engine, "I\u0131\u0130i", RegexOptions.IgnoreCase, "I\u0131\u0130i" };
            }
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60568", TestPlatforms.Android)]
        [MemberData(nameof(TurkishCulture_MatchesWordChar_MemberData))]
        public async Task TurkishCulture_MatchesWordChar(RegexEngine engine, string input, RegexOptions options, string expectedResult)
        {
            using (new ThreadCultureChange(new CultureInfo("tr-TR")))
            {
                Regex regex = await RegexHelpers.GetRegexAsync(engine, @"\w*", options);
                Assert.Equal(expectedResult, regex.Match(input).Value);
            }
        }

        public static IEnumerable<object[]> Match_In_Different_Cultures_TestData()
        {
            CultureInfo invariant = CultureInfo.InvariantCulture;
            CultureInfo enUS = new CultureInfo("en-US");
            CultureInfo turkish = new CultureInfo("tr-TR");

            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                foreach (RegexOptions option in new[] { RegexOptions.None, RegexOptions.RightToLeft })
                {
                    if (RegexHelpers.IsNonBacktracking(engine) && (option & RegexOptions.RightToLeft) != 0)
                    {
                        continue;
                    }

                    // \u0130 (Turkish I with dot) and \u0131 (Turkish i without dot) are unrelated characters in general

                    // Expected answers in the default en-US culture
                    yield return new object[] { "(?i:I)", option, engine, enUS, "xy\u0131ab", "" };
                    yield return new object[] { "(?i:iI+)", option, engine, enUS, "abcIIIxyz", "III" };
                    yield return new object[] { "(?i:iI+)", option, engine, enUS, "abcIi\u0130xyz", "Ii\u0130" };
                    yield return new object[] { "(?i:iI+)", option, engine, enUS, "abcI\u0130ixyz", "I\u0130i" };
                    yield return new object[] { "(?i:iI+)", option, engine, enUS, "abc\u0130IIxyz", "\u0130II" };
                    yield return new object[] { "(?i:iI+)", option, engine, enUS, "abc\u0130\u0131Ixyz", "" };
                    yield return new object[] { "(?i:iI+)", option, engine, enUS, "abc\u0130Iixyz", "\u0130Ii" };
                    yield return new object[] { "(?i:[^IJKLM]I)", option, engine, enUS, "ii\u0130i\u0131ab", "" };

                    // Expected answers in the invariant culture
                    yield return new object[] { "(?i:I)", option, engine, invariant, "xy\u0131ab", "" };
                    yield return new object[] { "(?i:iI+)", option, engine, invariant, "abcIIIxyz", "III" };
                    yield return new object[] { "(?i:iI+)", option, engine, invariant, "abc\u0130\u0131Ixyz", "" };

                    // Expected answers in the Turkish culture
                    //
                    // Android produces unexpected results for tr-TR
                    // https://github.com/dotnet/runtime/issues/60568
                    if (!PlatformDetection.IsAndroid)
                    {
                        yield return new object[] { "(?i:I)", option, engine, turkish, "xy\u0131ab", "\u0131" };
                        yield return new object[] { "(?i:iI+)", option, engine, turkish, "abcIIIxyz", "" };
                        yield return new object[] { "(?i:iI+)", option, engine, turkish, "abcIi\u0130xyz", "" };
                        yield return new object[] { "(?i:iI+)", option, engine, turkish, "abcI\u0130ixyz", "" };
                        yield return new object[] { "(?i:[^IJKLM]I)", option, engine, turkish, "ii\u0130i\u0131ab", "i\u0131" };
                        yield return new object[] { "(?i)\u0049", option, engine, turkish, "\u0131", "\u0131" };
                        yield return new object[] { "(?i)[a\u0049]", option, engine, turkish, "c\u0131c", "\u0131" };
                    }
                }

                // None and Compiled are separated into the Match_In_Different_Cultures_CriticalCases test
                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    foreach (object[] data in Match_In_Different_Cultures_CriticalCases_TestData_For(engine))
                    {
                        yield return data;
                    }
                }
            }
        }

        public static IEnumerable<object[]> Match_In_Different_Cultures_CriticalCases_TestData_For(RegexEngine engine)
        {
            CultureInfo invariant = CultureInfo.InvariantCulture;
            CultureInfo turkish = new CultureInfo("tr-TR");
            RegexOptions options = RegexOptions.None;

            // Expected answers in the invariant culture
            yield return new object[] { "(?i:iI+)", options, engine, invariant, "abcIi\u0130xyz", "Ii" };               // <-- failing for None, Compiled
            yield return new object[] { "(?i:iI+)", options, engine, invariant, "abcI\u0130ixyz", "" };                 // <-- failing for Compiled
            yield return new object[] { "(?i:iI+)", options, engine, invariant, "abc\u0130IIxyz", "II" };               // <-- failing for Compiled
            yield return new object[] { "(?i:iI+)", options, engine, invariant, "abc\u0130Iixyz", "Ii" };               // <-- failing for Compiled
            yield return new object[] { "(?i:[^IJKLM]I)", options, engine, invariant, "ii\u0130i\u0131ab", "\u0130i" }; // <-- failing for None, Compiled

            // Expected answers in the Turkish culture
            // Android produces unexpected results for tr-TR
            // https://github.com/dotnet/runtime/issues/60568
            if (!PlatformDetection.IsAndroid)
            {
                yield return new object[] { "(?i:iI+)", options, engine, turkish, "abc\u0130IIxyz", "\u0130II" };           // <-- failing for None, Compiled
                yield return new object[] { "(?i:iI+)", options, engine, turkish, "abc\u0130\u0131Ixyz", "\u0130\u0131I" }; // <-- failing for None, Compiled
                yield return new object[] { "(?i:iI+)", options, engine, turkish, "abc\u0130Iixyz", "\u0130I" };            // <-- failing for None, Compiled
            }
        }

        public static IEnumerable<object[]> Match_In_Different_Cultures_CriticalCases_TestData() =>
            Match_In_Different_Cultures_CriticalCases_TestData_For(RegexEngine.Interpreter).Union(Match_In_Different_Cultures_CriticalCases_TestData_For(RegexEngine.Compiled));

        [ActiveIssue("https://github.com/dotnet/runtime/issues/60899", TestPlatforms.Browser)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60697", TestPlatforms.iOS | TestPlatforms.tvOS)]
        [Theory]
        [MemberData(nameof(Match_In_Different_Cultures_TestData))]
        public async Task Match_In_Different_Cultures(string pattern, RegexOptions options, RegexEngine engine, CultureInfo culture, string input, string match_expected)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options, culture);
            Match match = r.Match(input);
            Assert.Equal(match_expected, match.Value);
        }

        [ActiveIssue("Incorrect treatment of IgnoreCase in Turkish and Invariant cultures, https://github.com/dotnet/runtime/issues/58956, https://github.com/dotnet/runtime/issues/58958 ")]
        [Theory]
        [MemberData(nameof(Match_In_Different_Cultures_CriticalCases_TestData))]
        public async Task Match_In_Different_Cultures_CriticalCases(string pattern, RegexOptions options, RegexEngine engine, CultureInfo culture, string input, string match_expected)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options, culture);
            Match match = r.Match(input);
            Assert.Equal(match_expected, match.Value);
        }

        [ActiveIssue("Incorrect result of match in complied mode in Invariant culture, https://github.com/dotnet/runtime/issues/58956")]
        [Fact]
        public void Match_InvariantCulture_None_vs_Compiled()
        {
            string pattern = "(?i:iI+)";
            string input = "abc\u0130IIxyz";
            Regex[] re = Create(pattern, CultureInfo.InvariantCulture, RegexOptions.None);
            Assert.Equal(re[0].Match(input).Value, re[1].Match(input).Value);
            Assert.Equal("II", re[0].Match(input).Value);
        }

        private const char Turkish_I_withDot = '\u0130';
        private const char Turkish_i_withoutDot = '\u0131';

        /// <summary>
        /// This test is to make sure that the generated IgnoreCaseRelation table for NonBacktracking does not need to be updated.
        /// It would need to be updated/regenerated if this test fails.
        /// </summary>
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60753")]
        [OuterLoop("May take several seconds due to large number of cultures tested")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [Fact]
        public void TestIgnoreCaseRelation()
        {
            // these 22 characters are considered case-insensitive by regex, while they are case-sensitive outside regex
            // but they are only case-sensitive in an asymmmetrical way: tolower(c)=c, tolower(toupper(c)) != c
            HashSet<char> treatedAsCaseInsensitive =
                 new("\u00B5\u017F\u0345\u03C2\u03D0\u03D1\u03D5\u03D6\u03F0\u03F1\u03F5\u1C80\u1C81\u1C82\u1C83\u1C84\u1C85\u1C86\u1C87\u1C88\u1E9B\u1FBE");

            foreach (char c in treatedAsCaseInsensitive)
            {
                char cU = char.ToUpper(c);
                Assert.NotEqual(c, cU);
                Assert.False(Regex.IsMatch(c.ToString(), cU.ToString(), RegexOptions.IgnoreCase));
            }

            Assert.False(Regex.IsMatch(Turkish_i_withoutDot.ToString(), "i", RegexOptions.IgnoreCase));

            // as baseline it is assumed the the invariant culture does not change
            HashSet<char>[] inv_table = ComputeIgnoreCaseTable(CultureInfo.InvariantCulture, treatedAsCaseInsensitive);
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            // expected difference between invariant and tr or az culture
            string tr_diff = string.Format("I:Ii/I{0},i:Ii/i{1},{1}:{1}/i{1},{0}:{0}/I{0}", Turkish_i_withoutDot, Turkish_I_withDot);

            // expected differnce between invariant and other cultures including the default en-US
            string default_diff = string.Format("I:Ii/Ii{0},i:Ii/Ii{0},{0}:{0}/Ii{0}", Turkish_I_withDot);

            // the expected difference between invariant culture and all other cultures is only for i,I,Turkish_I_withDot,Turkish_i_withoutDot
            // differentiate based on the TwoLetterISOLanguageName only (232 cases instead of 812)
            List<CultureInfo> testcultures = new();
            HashSet<string> done = new();
            for (int i = 0; i < cultures.Length; i++)
            {
                if (cultures[i] != CultureInfo.InvariantCulture && done.Add(cultures[i].TwoLetterISOLanguageName))
                {
                    testcultures.Add(cultures[i]);
                }
            }

            foreach (CultureInfo culture in testcultures)
            {
                HashSet<char>[] table = ComputeIgnoreCaseTable(culture, treatedAsCaseInsensitive);
                string diff = GetDiff(inv_table, table);
                if (culture.TwoLetterISOLanguageName == "tr" || culture.TwoLetterISOLanguageName == "az")
                {
                    // tr or az alphabet
                    Assert.Equal(tr_diff, diff);
                }
                else
                {
                    // all other alphabets are treated the same as en-US
                    Assert.Equal(default_diff, diff);
                }
            }
        }

        /// <summary>
        /// This test currently only works correctly in NonBacktracking mode.
        /// </summary>
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60753")]
        [OuterLoop("May take tens of seconds")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't support NonBacktracking")]
        [Theory]
        [InlineData(RegexHelpers.RegexOptionNonBacktracking)]
        public void TestIgnoreCaseRelationBorderCasesInNonBacktracking(RegexOptions options)
        {
            // these 22 characters are considered case-insensitive by regex, while they are case-sensitive outside regex
            // but they are only case-sensitive in an asymmmetrical way: tolower(c)=c, tolower(toupper(c)) != c
            HashSet<char> treatedAsCaseInsensitive =
                 new("\u00B5\u017F\u0345\u03C2\u03D0\u03D1\u03D5\u03D6\u03F0\u03F1\u03F5\u1C80\u1C81\u1C82\u1C83\u1C84\u1C85\u1C86\u1C87\u1C88\u1E9B\u1FBE");

            foreach (char c in treatedAsCaseInsensitive)
            {
                char cU = char.ToUpper(c);
                Assert.NotEqual(c, cU);
                Assert.False(Regex.IsMatch(c.ToString(), cU.ToString(), RegexOptions.IgnoreCase | options));
            }

            Assert.False(Regex.IsMatch(Turkish_i_withoutDot.ToString(), "i", RegexOptions.IgnoreCase | options));
            Assert.True(Regex.IsMatch(Turkish_I_withDot.ToString(), "i", RegexOptions.IgnoreCase | options));
            Assert.True(Regex.IsMatch(Turkish_I_withDot.ToString(), "i", RegexOptions.IgnoreCase | options));
            Assert.False(Regex.IsMatch(Turkish_I_withDot.ToString(), "i", RegexOptions.IgnoreCase | options | RegexOptions.CultureInvariant));

            // Turkish i without dot is not considered case-sensitive in the default en-US culture
            treatedAsCaseInsensitive.Add(Turkish_i_withoutDot);

            List<char> caseSensitiveChars = new();
            for (char c = '\0'; c < '\uFFFF'; c++)
            {
                if (!treatedAsCaseInsensitive.Contains(c) && char.ToUpper(c) != char.ToLower(c))
                {
                    caseSensitiveChars.Add(c);
                }
            }

            // test all case-sensitive characters exhaustively in NonBacktracking mode
            foreach (char c in caseSensitiveChars)
            {
                Assert.True(Regex.IsMatch(char.ToUpper(c).ToString() + char.ToLower(c).ToString(),
                    c.ToString() + c.ToString(), RegexOptions.IgnoreCase | options));
            }
        }

        /// <summary>
        /// Maps each character c to the set of all of its equivalent characters if case is ignored or null if c in case-insensitive
        /// </summary>
        /// <param name="culture">ignoring case wrt this culture</param>
        /// <param name="treatedAsCaseInsensitive">characters that are otherwise case-sensitive but not in a regex</param>
        private static HashSet<char>[] ComputeIgnoreCaseTable(CultureInfo culture, HashSet<char> treatedAsCaseInsensitive)
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = culture;
            var ignoreCase = new HashSet<char>[0x10000];

            for (uint i = 0; i <= 0xFFFF; i++)
            {
                char c = (char)i;
                char cU = char.ToUpper(c);
                char cL = char.ToLower(c);
                // Turkish i without dot is only considered case-sensitive in tr and az languages
                if (treatedAsCaseInsensitive.Contains(c) ||
                    (c == Turkish_i_withoutDot && culture.TwoLetterISOLanguageName != "tr" && culture.TwoLetterISOLanguageName != "az"))
                {
                    continue;
                }

                if (cU != cL)
                {
                    HashSet<char> set = (ignoreCase[c] == null ? (ignoreCase[cU] == null ? (ignoreCase[cL] == null ? new HashSet<char>()
                                                     : ignoreCase[cL]) : ignoreCase[cU]) : ignoreCase[c]);
                    set.Add(c);
                    set.Add(cU);
                    set.Add(cL);
                    ignoreCase[c] = set;
                    ignoreCase[cL] = set;
                    ignoreCase[cU] = set;
                }
            }

            CultureInfo.CurrentCulture = ci;
            return ignoreCase;
        }

        /// <summary>Represent the difference between the two tables as a special string</summary>
        private static string GetDiff(HashSet<char>[] table1, HashSet<char>[] table2)
        {
            List<string> diffs = new();
            Func<HashSet<char>, int, string> F = (s, i) =>
            {
                if (s == null)
                {
                    return ((char)i).ToString();
                }

                var elems = new List<char>(s);
                elems.Sort();
                return new string(elems.ToArray());
            };

            for (int i = 0; i <= 0xFFFF; i++)
            {
                string s1 = F(table1[i], i);
                string s2 = F(table2[i], i);
                if (s1 != s2)
                {
                    diffs.Add($"{(char)i}:{s1}/{s2}");
                }
            }

            return string.Join(",", diffs.ToArray());
        }
    }
}
