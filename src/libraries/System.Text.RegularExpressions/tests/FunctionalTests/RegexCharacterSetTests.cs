// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    [OuterLoop]
    public class RegexCharacterSetTests
    {
        public static IEnumerable<object[]> SetInclusionsExpected_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, @"a", RegexOptions.IgnoreCase, new[] { 'a', 'A' } };
                yield return new object[] { engine, @"ac", RegexOptions.None, new[] { 'a', 'c' } };
                yield return new object[] { engine, @"\u00E5\u00C5\u212B", RegexOptions.None, new[] { '\u00E5', '\u00C5', '\u212B' } };
                yield return new object[] { engine, @"ace", RegexOptions.None, new[] { 'a', 'c', 'e' } };
                yield return new object[] { engine, @"aceg", RegexOptions.None, new[] { 'a', 'c', 'e', 'g' } };
                yield return new object[] { engine, @"aceg", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'c', 'C', 'e', 'E', 'g', 'G' } };
                yield return new object[] { engine, @"\u00A9", RegexOptions.None, new[] { '\u00A9' } };
                yield return new object[] { engine, @"\u00A9", RegexOptions.IgnoreCase, new[] { '\u00A9' } };
                yield return new object[] { engine, @"\u00FD\u00FF", RegexOptions.None, new[] { '\u00FD', '\u00FF' } };
                yield return new object[] { engine, @"\u00FE\u0080", RegexOptions.None, new[] { '\u00FE', '\u0080' } };
                yield return new object[] { engine, @"\u0080\u0082", RegexOptions.None, new[] { '\u0080', '\u0082' } };
                yield return new object[] { engine, @"az", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z' } };
                yield return new object[] { engine, @"azY", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y' } };
                yield return new object[] { engine, @"azY", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y' } };
                yield return new object[] { engine, @"azY\u00A9", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9' } };
                yield return new object[] { engine, @"azY\u00A9", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9' } };
                yield return new object[] { engine, @"azY\u00A9\u05D0", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9', '\u05D0' } };
                yield return new object[] { engine, @"azY\u00A9\u05D0", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9', '\u05D0' } };
                yield return new object[] { engine, @"a ", RegexOptions.None, new[] { 'a', ' ' } };
                yield return new object[] { engine, @"a \t\r", RegexOptions.None, new[] { 'a', ' ', '\t', '\r' } };
                yield return new object[] { engine, @"aeiou", RegexOptions.None, new[] { 'a', 'e', 'i', 'o', 'u' } };
                yield return new object[] { engine, @"\u0000aeiou\u00FF", RegexOptions.None, new[] { '\u0000', 'a', 'e', 'i', 'o', 'u', '\u00FF' } };
                yield return new object[] { engine, @"a-a", RegexOptions.None, new[] { 'a' } };
                yield return new object[] { engine, @"ab", RegexOptions.None, new[] { 'a', 'b' } };
                yield return new object[] { engine, @"a-b", RegexOptions.None, new[] { 'a', 'b' } };
                yield return new object[] { engine, @"abc", RegexOptions.None, new[] { 'a', 'b', 'c' } };
                yield return new object[] { engine, @"1369", RegexOptions.None, new[] { '1', '3', '6', '9' } };
                yield return new object[] { engine, @"ACEGIKMOQSUWY", RegexOptions.None, new[] { 'A', 'C', 'E', 'G', 'I', 'K', 'M', 'O', 'Q', 'S', 'U', 'W', 'Y' } };
                yield return new object[] { engine, @"abcAB", RegexOptions.None, new[] { 'A', 'B', 'a', 'b', 'c' } };
                yield return new object[] { engine, @"a-c", RegexOptions.None, new[] { 'a', 'b', 'c' } };
                yield return new object[] { engine, @"a-fA-F", RegexOptions.None, new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' } };
                yield return new object[] { engine, @"a-fA-F0-9", RegexOptions.None, new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' } };
                yield return new object[] { engine, @"X-b", RegexOptions.None, new[] { 'X', 'Y', 'Z', '[', '\\', ']', '^', '_', '`', 'a', 'b' } };
                yield return new object[] { engine, @"\u0083\u00DE-\u00E1", RegexOptions.None, new[] { '\u0083', '\u00DE', '\u00DF', '\u00E0', '\u00E1' } };
                yield return new object[] { engine, @"\u007A-\u0083\u00DE-\u00E1", RegexOptions.None, new[] { '\u007A', '\u007B', '\u007C', '\u007D', '\u007E', '\u007F', '\u0080', '\u0081', '\u0082', '\u0083', '\u00DE', '\u00DF', '\u00E0', '\u00E1' } };
                yield return new object[] { engine, @"\u05D0", RegexOptions.None, new[] { '\u05D0' } };
                yield return new object[] { engine, @"a\u05D0", RegexOptions.None, new[] { 'a', '\u05D0' } };
                yield return new object[] { engine, @"\uFFFC-\uFFFF", RegexOptions.None, new[] { '\uFFFC', '\uFFFD', '\uFFFE', '\uFFFF' } };
                yield return new object[] { engine, @"[a-z-[d-w-[m-o]]]", RegexOptions.None, new[] { 'a', 'b', 'c', 'm', 'n', 'n', 'o', 'x', 'y', 'z' } };
                yield return new object[] { engine, @"\p{IsBasicLatin}-[\x00-\x7F]", RegexOptions.None, new char[0] };
                yield return new object[] { engine, @"[0-9-[2468]]", RegexOptions.None, new[] { '0', '1', '3', '5', '7', '9' } };
                yield return new object[] { engine, @"[\u1000-\u1001\u3000-\u3002\u5000-\u5003]", RegexOptions.None, new[] { '\u1000', '\u1001', '\u3000', '\u3001', '\u3002', '\u5000', '\u5001', '\u5002', '\u5003' } };
            }
        }

        [Theory]
        [MemberData(nameof(SetInclusionsExpected_MemberData))]
        public async Task SetInclusionsExpected(RegexEngine engine, string set, RegexOptions options, char[] expectedIncluded)
        {
            bool hasBracket = set.Contains("[");
            if (hasBracket)
            {
                await ValidateSetAsync(engine, set, options, new HashSet<char>(expectedIncluded), null, validateEveryChar: true);
            }
            else
            {
                await ValidateSetAsync(engine, $"[{set}]", options, new HashSet<char>(expectedIncluded), null, validateEveryChar: true);
                await ValidateSetAsync(engine, $"[^{set}]", options, null, new HashSet<char>(expectedIncluded), validateEveryChar: true);
            }
        }

        public static IEnumerable<object[]> SetExclusionsExpected_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, @"[^1234-[3456]]", RegexOptions.None, new[] { '1', '2', '3', '4', '5', '6' } };
            }
        }

        [Theory]
        [MemberData(nameof(SetExclusionsExpected_MemberData))]
        public async Task SetExclusionsExpected(RegexEngine engine, string set, RegexOptions options, char[] expectedExcluded)
        {
            await ValidateSetAsync(engine, set, options, null, new HashSet<char>(expectedExcluded), validateEveryChar: true);
        }

        public static IEnumerable<object[]> SingleExpected_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, '\0' };
                yield return new object[] { engine, '\uFFFF' };
                yield return new object[] { engine, 'a' };
                yield return new object[] { engine, '5' };
                yield return new object[] { engine, '\u00FF' };
                yield return new object[] { engine, '\u0080' };
                yield return new object[] { engine, '\u0100' };
            }
        }

        [Theory]
        [MemberData(nameof(SingleExpected_MemberData))]
        public async Task SingleExpected(RegexEngine engine, char c)
        {
            string s = $@"\u{(int)c:X4}";
            var set = new HashSet<char>() { c };

            // One
            await ValidateSetAsync(engine, $"{s}", RegexOptions.None, set, null);
            await ValidateSetAsync(engine, $"[{s}]", RegexOptions.None, set, null);
            await ValidateSetAsync(engine, $"[^{s}]", RegexOptions.None, null, set);

            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                // Positive lookahead
                await ValidateSetAsync(engine, $"(?={s}){s}", RegexOptions.None, set, null);
                await ValidateSetAsync(engine, $"(?=[^{s}])[^{s}]", RegexOptions.None, null, set);

                // Negative lookahead
                await ValidateSetAsync(engine, $"(?![^{s}]){s}", RegexOptions.None, set, null);
                await ValidateSetAsync(engine, $"(?![{s}])[^{s}]", RegexOptions.None, null, set);
            }

            // Concatenation
            await ValidateSetAsync(engine, $"[{s}{s}]", RegexOptions.None, set, null);
            await ValidateSetAsync(engine, $"[^{s}{s}{s}]", RegexOptions.None, null, set);

            // Alternation
            await ValidateSetAsync(engine, $"{s}|{s}", RegexOptions.None, set, null);
            await ValidateSetAsync(engine, $"[^{s}]|[^{s}]|[^{s}]", RegexOptions.None, null, set);
            await ValidateSetAsync(engine, $"{s}|[^{s}]", RegexOptions.None, null, new HashSet<char>());
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task AllEmptySets(RegexEngine engine)
        {
            var set = new HashSet<char>();

            await ValidateSetAsync(engine, @"[\u0000-\uFFFF]", RegexOptions.None, null, set);
            await ValidateSetAsync(engine, @"[\u0000-\uFFFFa-z]", RegexOptions.None, null, set);
            await ValidateSetAsync(engine, @"[\u0000-\u1000\u1001-\u2002\u2003-\uFFFF]", RegexOptions.None, null, set);
            await ValidateSetAsync(engine, @"[\u0000-\uFFFE\u0001-\uFFFF]", RegexOptions.None, null, set, validateEveryChar: true);
            foreach (string all in new[] { @"[\d\D]", @"[\D\d]", @"[\w\W]", @"[\W\w]", @"[\s\S]", @"[\S\s]", })
            {
                await ValidateSetAsync(engine, all, RegexOptions.None, null, new HashSet<char>(), validateEveryChar: true);
            }

            await ValidateSetAsync(engine, @"[^\u0000-\uFFFF]", RegexOptions.None, set, null);
            await ValidateSetAsync(engine, @"[^\u0000-\uFFFFa-z]", RegexOptions.None, set, null);
            await ValidateSetAsync(engine, @"[^\u0000-\uFFFE\u0001-\uFFFF]", RegexOptions.None, set, null);
            await ValidateSetAsync(engine, @"[^\u0000-\u1000\u1001-\u2002\u2003-\uFFFF]", RegexOptions.None, set, null, validateEveryChar: true);
            foreach (string empty in new[] { @"[^\d\D]", @"[^\D\d]", @"[^\w\W]", @"[^\W\w]", @"[^\s\S]", @"[^\S\s]", })
            {
                await ValidateSetAsync(engine, empty, RegexOptions.None, set, null, validateEveryChar: true);
            }
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task AllButOneSets(RegexEngine engine)
        {
            await ValidateSetAsync(engine, @"[\u0000-\uFFFE]", RegexOptions.None, null, new HashSet<char>() { '\uFFFF' });
            await ValidateSetAsync(engine, @"[\u0001-\uFFFF]", RegexOptions.None, null, new HashSet<char>() { '\u0000' });
            await ValidateSetAsync(engine, @"[\u0000-ac-\uFFFF]", RegexOptions.None, null, new HashSet<char>() { 'b' }, validateEveryChar: true);
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task DotInclusionsExpected(RegexEngine engine)
        {
            await ValidateSetAsync(engine, ".", RegexOptions.None, null, new HashSet<char>() { '\n' });
            await ValidateSetAsync(engine, ".", RegexOptions.IgnoreCase, null, new HashSet<char>() { '\n' });
            await ValidateSetAsync(engine, ".", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, null, new HashSet<char>() { '\n' }, validateEveryChar: true);

            await ValidateSetAsync(engine, ".", RegexOptions.Singleline, null, new HashSet<char>());
            await ValidateSetAsync(engine, ".", RegexOptions.Singleline | RegexOptions.IgnoreCase, null, new HashSet<char>());
            await ValidateSetAsync(engine, ".", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, null, new HashSet<char>(), validateEveryChar: true);
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task WhitespaceInclusionsExpected(RegexEngine engine)
        {
            HashSet<char> whitespaceInclusions = ComputeIncludedSet(char.IsWhiteSpace);
            await ValidateSetAsync(engine, @"[\s]", RegexOptions.None, whitespaceInclusions, null);
            await ValidateSetAsync(engine, @"[^\s]", RegexOptions.None, null, whitespaceInclusions);
            await ValidateSetAsync(engine, @"[\S]", RegexOptions.None, null, whitespaceInclusions);
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task DigitInclusionsExpected(RegexEngine engine)
        {
            HashSet<char> digitInclusions = ComputeIncludedSet(char.IsDigit);
            await ValidateSetAsync(engine, @"[\d]", RegexOptions.None, digitInclusions, null);
            await ValidateSetAsync(engine, @"[^\d]", RegexOptions.None, null, digitInclusions);
            await ValidateSetAsync(engine, @"[\D]", RegexOptions.None, null, digitInclusions);
        }

        public static IEnumerable<object[]> UnicodeCategoryInclusionsExpected_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, @"\p{Lu}", new[] { UnicodeCategory.UppercaseLetter } };
                yield return new object[] { engine, @"\p{S}", new[] { UnicodeCategory.CurrencySymbol, UnicodeCategory.MathSymbol, UnicodeCategory.ModifierSymbol, UnicodeCategory.OtherSymbol } };
                yield return new object[] { engine, @"\p{Lu}\p{Zl}", new[] { UnicodeCategory.UppercaseLetter, UnicodeCategory.LineSeparator } };
                yield return new object[] { engine, @"\w", new[] { UnicodeCategory.LowercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.OtherLetter, UnicodeCategory.ModifierLetter, UnicodeCategory.NonSpacingMark, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.ConnectorPunctuation } };
            }
        }

        [Theory]
        [MemberData(nameof(UnicodeCategoryInclusionsExpected_MemberData))]
        public async Task UnicodeCategoryInclusionsExpected(RegexEngine engine, string set, UnicodeCategory[] categories)
        {
            HashSet<char> categoryInclusions = ComputeIncludedSet(c => Array.IndexOf(categories, char.GetUnicodeCategory(c)) >= 0);
            await ValidateSetAsync(engine, $"[{set}]", RegexOptions.None, categoryInclusions, null);
            await ValidateSetAsync(engine, $"[^{set}]", RegexOptions.None, null, categoryInclusions);
        }

        public static IEnumerable<object[]> NamedBlocksInclusionsExpected_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, @"\p{IsBasicLatin}", new[] { 0x0000, 0x007F } };
                yield return new object[] { engine, @"\p{IsLatin-1Supplement}", new[] { 0x0080, 0x00FF } };
                yield return new object[] { engine, @"\p{IsLatinExtended-A}", new[] { 0x0100, 0x017F } };
                yield return new object[] { engine, @"\p{IsLatinExtended-B}", new[] { 0x0180, 0x024F } };
                yield return new object[] { engine, @"\p{IsIPAExtensions}", new[] { 0x0250, 0x02AF } };
                yield return new object[] { engine, @"\p{IsSpacingModifierLetters}", new[] { 0x02B0, 0x02FF } };
                yield return new object[] { engine, @"\p{IsCombiningDiacriticalMarks}", new[] { 0x0300, 0x036F } };
                yield return new object[] { engine, @"\p{IsGreek}", new[] { 0x0370, 0x03FF } };
                yield return new object[] { engine, @"\p{IsCyrillic}", new[] { 0x0400, 0x04FF } };
                yield return new object[] { engine, @"\p{IsCyrillicSupplement}", new[] { 0x0500, 0x052F } };
                yield return new object[] { engine, @"\p{IsArmenian}", new[] { 0x0530, 0x058F } };
                yield return new object[] { engine, @"\p{IsHebrew}", new[] { 0x0590, 0x05FF } };
                yield return new object[] { engine, @"\p{IsArabic}", new[] { 0x0600, 0x06FF } };
                yield return new object[] { engine, @"\p{IsSyriac}", new[] { 0x0700, 0x074F } };
                yield return new object[] { engine, @"\p{IsThaana}", new[] { 0x0780, 0x07BF } };
                yield return new object[] { engine, @"\p{IsDevanagari}", new[] { 0x0900, 0x097F } };
                yield return new object[] { engine, @"\p{IsBengali}", new[] { 0x0980, 0x09FF } };
                yield return new object[] { engine, @"\p{IsGurmukhi}", new[] { 0x0A00, 0x0A7F } };
                yield return new object[] { engine, @"\p{IsGujarati}", new[] { 0x0A80, 0x0AFF } };
                yield return new object[] { engine, @"\p{IsOriya}", new[] { 0x0B00, 0x0B7F } };
                yield return new object[] { engine, @"\p{IsTamil}", new[] { 0x0B80, 0x0BFF } };
                yield return new object[] { engine, @"\p{IsTelugu}", new[] { 0x0C00, 0x0C7F } };
                yield return new object[] { engine, @"\p{IsKannada}", new[] { 0x0C80, 0x0CFF } };
                yield return new object[] { engine, @"\p{IsMalayalam}", new[] { 0x0D00, 0x0D7F } };
                yield return new object[] { engine, @"\p{IsSinhala}", new[] { 0x0D80, 0x0DFF } };
                yield return new object[] { engine, @"\p{IsThai}", new[] { 0x0E00, 0x0E7F } };
                yield return new object[] { engine, @"\p{IsLao}", new[] { 0x0E80, 0x0EFF } };
                yield return new object[] { engine, @"\p{IsTibetan}", new[] { 0x0F00, 0x0FFF } };
                yield return new object[] { engine, @"\p{IsMyanmar}", new[] { 0x1000, 0x109F } };
                yield return new object[] { engine, @"\p{IsGeorgian}", new[] { 0x10A0, 0x10FF } };
                yield return new object[] { engine, @"\p{IsHangulJamo}", new[] { 0x1100, 0x11FF } };
                yield return new object[] { engine, @"\p{IsEthiopic}", new[] { 0x1200, 0x137F } };
                yield return new object[] { engine, @"\p{IsCherokee}", new[] { 0x13A0, 0x13FF } };
                yield return new object[] { engine, @"\p{IsUnifiedCanadianAboriginalSyllabics}", new[] { 0x1400, 0x167F } };
                yield return new object[] { engine, @"\p{IsOgham}", new[] { 0x1680, 0x169F } };
                yield return new object[] { engine, @"\p{IsRunic}", new[] { 0x16A0, 0x16FF } };
                yield return new object[] { engine, @"\p{IsTagalog}", new[] { 0x1700, 0x171F } };
                yield return new object[] { engine, @"\p{IsHanunoo}", new[] { 0x1720, 0x173F } };
                yield return new object[] { engine, @"\p{IsBuhid}", new[] { 0x1740, 0x175F } };
                yield return new object[] { engine, @"\p{IsTagbanwa}", new[] { 0x1760, 0x177F } };
                yield return new object[] { engine, @"\p{IsKhmer}", new[] { 0x1780, 0x17FF } };
                yield return new object[] { engine, @"\p{IsMongolian}", new[] { 0x1800, 0x18AF } };
                yield return new object[] { engine, @"\p{IsLimbu}", new[] { 0x1900, 0x194F } };
                yield return new object[] { engine, @"\p{IsTaiLe}", new[] { 0x1950, 0x197F } };
                yield return new object[] { engine, @"\p{IsKhmerSymbols}", new[] { 0x19E0, 0x19FF } };
                yield return new object[] { engine, @"\p{IsPhoneticExtensions}", new[] { 0x1D00, 0x1D7F } };
                yield return new object[] { engine, @"\p{IsLatinExtendedAdditional}", new[] { 0x1E00, 0x1EFF } };
                yield return new object[] { engine, @"\p{IsGreekExtended}", new[] { 0x1F00, 0x1FFF } };
                yield return new object[] { engine, @"\p{IsGeneralPunctuation}", new[] { 0x2000, 0x206F } };
                yield return new object[] { engine, @"\p{IsSuperscriptsandSubscripts}", new[] { 0x2070, 0x209F } };
                yield return new object[] { engine, @"\p{IsCurrencySymbols}", new[] { 0x20A0, 0x20CF } };
                yield return new object[] { engine, @"\p{IsCombiningDiacriticalMarksforSymbols}", new[] { 0x20D0, 0x20FF } };
                yield return new object[] { engine, @"\p{IsLetterlikeSymbols}", new[] { 0x2100, 0x214F } };
                yield return new object[] { engine, @"\p{IsNumberForms}", new[] { 0x2150, 0x218F } };
                yield return new object[] { engine, @"\p{IsArrows}", new[] { 0x2190, 0x21FF } };
                yield return new object[] { engine, @"\p{IsMathematicalOperators}", new[] { 0x2200, 0x22FF } };
                yield return new object[] { engine, @"\p{IsMiscellaneousTechnical}", new[] { 0x2300, 0x23FF } };
                yield return new object[] { engine, @"\p{IsControlPictures}", new[] { 0x2400, 0x243F } };
                yield return new object[] { engine, @"\p{IsOpticalCharacterRecognition}", new[] { 0x2440, 0x245F } };
                yield return new object[] { engine, @"\p{IsEnclosedAlphanumerics}", new[] { 0x2460, 0x24FF } };
                yield return new object[] { engine, @"\p{IsBoxDrawing}", new[] { 0x2500, 0x257F } };
                yield return new object[] { engine, @"\p{IsBlockElements}", new[] { 0x2580, 0x259F } };
                yield return new object[] { engine, @"\p{IsGeometricShapes}", new[] { 0x25A0, 0x25FF } };
                yield return new object[] { engine, @"\p{IsMiscellaneousSymbols}", new[] { 0x2600, 0x26FF } };
                yield return new object[] { engine, @"\p{IsDingbats}", new[] { 0x2700, 0x27BF } };
                yield return new object[] { engine, @"\p{IsMiscellaneousMathematicalSymbols-A}", new[] { 0x27C0, 0x27EF } };
                yield return new object[] { engine, @"\p{IsSupplementalArrows-A}", new[] { 0x27F0, 0x27FF } };
                yield return new object[] { engine, @"\p{IsBraillePatterns}", new[] { 0x2800, 0x28FF } };
                yield return new object[] { engine, @"\p{IsSupplementalArrows-B}", new[] { 0x2900, 0x297F } };
                yield return new object[] { engine, @"\p{IsMiscellaneousMathematicalSymbols-B}", new[] { 0x2980, 0x29FF } };
                yield return new object[] { engine, @"\p{IsSupplementalMathematicalOperators}", new[] { 0x2A00, 0x2AFF } };
                yield return new object[] { engine, @"\p{IsMiscellaneousSymbolsandArrows}", new[] { 0x2B00, 0x2BFF } };
                yield return new object[] { engine, @"\p{IsCJKRadicalsSupplement}", new[] { 0x2E80, 0x2EFF } };
                yield return new object[] { engine, @"\p{IsKangxiRadicals}", new[] { 0x2F00, 0x2FDF } };
                yield return new object[] { engine, @"\p{IsIdeographicDescriptionCharacters}", new[] { 0x2FF0, 0x2FFF } };
                yield return new object[] { engine, @"\p{IsCJKSymbolsandPunctuation}", new[] { 0x3000, 0x303F } };
                yield return new object[] { engine, @"\p{IsHiragana}", new[] { 0x3040, 0x309F } };
                yield return new object[] { engine, @"\p{IsKatakana}", new[] { 0x30A0, 0x30FF } };
                yield return new object[] { engine, @"\p{IsBopomofo}", new[] { 0x3100, 0x312F } };
                yield return new object[] { engine, @"\p{IsHangulCompatibilityJamo}", new[] { 0x3130, 0x318F } };
                yield return new object[] { engine, @"\p{IsKanbun}", new[] { 0x3190, 0x319F } };
                yield return new object[] { engine, @"\p{IsBopomofoExtended}", new[] { 0x31A0, 0x31BF } };
                yield return new object[] { engine, @"\p{IsKatakanaPhoneticExtensions}", new[] { 0x31F0, 0x31FF } };
                yield return new object[] { engine, @"\p{IsEnclosedCJKLettersandMonths}", new[] { 0x3200, 0x32FF } };
                yield return new object[] { engine, @"\p{IsCJKCompatibility}", new[] { 0x3300, 0x33FF } };
                yield return new object[] { engine, @"\p{IsCJKUnifiedIdeographsExtensionA}", new[] { 0x3400, 0x4DBF } };
                yield return new object[] { engine, @"\p{IsYijingHexagramSymbols}", new[] { 0x4DC0, 0x4DFF } };
                yield return new object[] { engine, @"\p{IsCJKUnifiedIdeographs}", new[] { 0x4E00, 0x9FFF } };
                yield return new object[] { engine, @"\p{IsYiSyllables}", new[] { 0xA000, 0xA48F } };
                yield return new object[] { engine, @"\p{IsYiRadicals}", new[] { 0xA490, 0xA4CF } };
                yield return new object[] { engine, @"\p{IsHangulSyllables}", new[] { 0xAC00, 0xD7AF } };
                yield return new object[] { engine, @"\p{IsHighSurrogates}", new[] { 0xD800, 0xDB7F } };
                yield return new object[] { engine, @"\p{IsHighPrivateUseSurrogates}", new[] { 0xDB80, 0xDBFF } };
                yield return new object[] { engine, @"\p{IsLowSurrogates}", new[] { 0xDC00, 0xDFFF } };
                yield return new object[] { engine, @"\p{IsPrivateUse}", new[] { 0xE000, 0xF8FF } };
                yield return new object[] { engine, @"\p{IsCJKCompatibilityIdeographs}", new[] { 0xF900, 0xFAFF } };
                yield return new object[] { engine, @"\p{IsAlphabeticPresentationForms}", new[] { 0xFB00, 0xFB4F } };
                yield return new object[] { engine, @"\p{IsArabicPresentationForms-A}", new[] { 0xFB50, 0xFDFF } };
                yield return new object[] { engine, @"\p{IsVariationSelectors}", new[] { 0xFE00, 0xFE0F } };
                yield return new object[] { engine, @"\p{IsCombiningHalfMarks}", new[] { 0xFE20, 0xFE2F } };
                yield return new object[] { engine, @"\p{IsCJKCompatibilityForms}", new[] { 0xFE30, 0xFE4F } };
                yield return new object[] { engine, @"\p{IsSmallFormVariants}", new[] { 0xFE50, 0xFE6F } };
                yield return new object[] { engine, @"\p{IsArabicPresentationForms-B}", new[] { 0xFE70, 0xFEFF } };
                yield return new object[] { engine, @"\p{IsHalfwidthandFullwidthForms}", new[] { 0xFF00, 0xFFEF } };
                yield return new object[] { engine, @"\p{IsSpecials}", new[] { 0xFFF0, 0xFFFF } };
                yield return new object[] { engine, @"\p{IsRunic}\p{IsHebrew}", new[] { 0x0590, 0x05FF, 0x16A0, 0x16FF } };
                yield return new object[] { engine, @"abx-z\p{IsRunic}\p{IsHebrew}", new[] { 0x0590, 0x05FF, 0x16A0, 0x16FF, 'a', 'a', 'b', 'b', 'x', 'x', 'y', 'z' } };
            }
        }

        [Theory]
        [MemberData(nameof(NamedBlocksInclusionsExpected_MemberData))]
        public async Task NamedBlocksInclusionsExpected(RegexEngine engine, string set, int[] ranges)
        {
            var included = new HashSet<char>();
            for (int i = 0; i < ranges.Length - 1; i += 2)
            {
                ComputeIncludedSet(c => c >= ranges[i] && c <= ranges[i + 1], included);
            }

            await ValidateSetAsync(engine, $"[{set}]", RegexOptions.None, included, null);
            await ValidateSetAsync(engine, $"[^{set}]", RegexOptions.None, null, included);
        }

        public static IEnumerable<object[]> UnicodeCategoriesInclusionsExpected_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "Cc", UnicodeCategory.Control };
                yield return new object[] { engine, "Cf", UnicodeCategory.Format };
                yield return new object[] { engine, "Cn", UnicodeCategory.OtherNotAssigned };
                yield return new object[] { engine, "Co", UnicodeCategory.PrivateUse };
                yield return new object[] { engine, "Cs", UnicodeCategory.Surrogate };
                yield return new object[] { engine, "Ll", UnicodeCategory.LowercaseLetter };
                yield return new object[] { engine, "Lm", UnicodeCategory.ModifierLetter };
                yield return new object[] { engine, "Lo", UnicodeCategory.OtherLetter };
                yield return new object[] { engine, "Lt", UnicodeCategory.TitlecaseLetter };
                yield return new object[] { engine, "Lu", UnicodeCategory.UppercaseLetter };
                yield return new object[] { engine, "Mc", UnicodeCategory.SpacingCombiningMark };
                yield return new object[] { engine, "Me", UnicodeCategory.EnclosingMark };
                yield return new object[] { engine, "Mn", UnicodeCategory.NonSpacingMark };
                yield return new object[] { engine, "Nd", UnicodeCategory.DecimalDigitNumber };
                yield return new object[] { engine, "Nl", UnicodeCategory.LetterNumber };
                yield return new object[] { engine, "No", UnicodeCategory.OtherNumber };
                yield return new object[] { engine, "Pc", UnicodeCategory.ConnectorPunctuation };
                yield return new object[] { engine, "Pd", UnicodeCategory.DashPunctuation };
                yield return new object[] { engine, "Pe", UnicodeCategory.ClosePunctuation };
                yield return new object[] { engine, "Po", UnicodeCategory.OtherPunctuation };
                yield return new object[] { engine, "Ps", UnicodeCategory.OpenPunctuation };
                yield return new object[] { engine, "Pf", UnicodeCategory.FinalQuotePunctuation };
                yield return new object[] { engine, "Pi", UnicodeCategory.InitialQuotePunctuation };
                yield return new object[] { engine, "Sc", UnicodeCategory.CurrencySymbol };
                yield return new object[] { engine, "Sk", UnicodeCategory.ModifierSymbol };
                yield return new object[] { engine, "Sm", UnicodeCategory.MathSymbol };
                yield return new object[] { engine, "So", UnicodeCategory.OtherSymbol };
                yield return new object[] { engine, "Zl", UnicodeCategory.LineSeparator };
                yield return new object[] { engine, "Zp", UnicodeCategory.ParagraphSeparator };
                yield return new object[] { engine, "Zs", UnicodeCategory.SpaceSeparator };
            }
        }

        [Theory]
        [MemberData(nameof(UnicodeCategoriesInclusionsExpected_MemberData))]
        public async Task UnicodeCategoriesInclusionsExpected(RegexEngine engine, string generalCategory, UnicodeCategory unicodeCategory)
        {
            Regex r;

            char[] allChars = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).ToArray();
            int expectedInCategory = allChars.Count(c => char.GetUnicodeCategory(c) == unicodeCategory);
            int expectedNotInCategory = allChars.Length - expectedInCategory;

            r = await RegexHelpers.GetRegexAsync(engine, @$"\p{{{generalCategory}}}");
            Assert.Equal(expectedInCategory, r.Matches(string.Concat(allChars)).Count);

            r = await RegexHelpers.GetRegexAsync(engine, (@$"\P{{{generalCategory}}}"));
            Assert.Equal(expectedNotInCategory, r.Matches(string.Concat(allChars)).Count);
        }

        [Theory]
        [InlineData("ab", 1, false)]
        [InlineData("a b", 1, true)]
        [InlineData("a b", 2, true)]
        [InlineData("\u200Da", 1, false)]
        [InlineData("\u200D\u200C", 1, false)]
        [InlineData("\u200Ca", 1, false)]
        [InlineData("\u200C a", 1, true)]
        public void IsBoundary_ReturnsExpectedResult(string text, int pos, bool expectedBoundary)
        {
            var r = new DerivedRunner(text);
            Assert.Equal(expectedBoundary, r.IsBoundary(pos, 0, text.Length));
        }

        private static HashSet<char> ComputeIncludedSet(Func<char, bool> func)
        {
            var included = new HashSet<char>();
            ComputeIncludedSet(func, included);
            return included;
        }

        private static void ComputeIncludedSet(Func<char, bool> func, HashSet<char> included)
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (func((char)i))
                {
                    included.Add((char)i);
                }
            }
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task ValidateValidateSet(RegexEngine engine)
        {
            await Assert.ThrowsAsync<XunitException>(() => ValidateSetAsync(engine, "[a]", RegexOptions.None, new HashSet<char>() { 'b' }, null));
            await Assert.ThrowsAsync<XunitException>(() => ValidateSetAsync(engine, "[a]", RegexOptions.None, new HashSet<char>() { 'b' }, null, validateEveryChar: true));

            await Assert.ThrowsAsync<XunitException>(() => ValidateSetAsync(engine, "[b]", RegexOptions.None, null, new HashSet<char>() { 'b' }));
            await Assert.ThrowsAsync<XunitException>(() => ValidateSetAsync(engine, "[b]", RegexOptions.None, null, new HashSet<char>() { 'b' }, validateEveryChar: true));
        }

        [Fact]
        public void RegexRunner_Legacy_CharInSet()
        {
            Assert.True(DerivedRunner.CharInSet('a', "ab", ""));
            Assert.False(DerivedRunner.CharInSet('x', "ab", ""));

            Assert.True(DerivedRunner.CharInSet('x', "\0\0ab", ""));
            Assert.False(DerivedRunner.CharInSet('a', "\0\0ab", ""));

            Assert.True(DerivedRunner.CharInSet('4', "", "\x0009"));
            Assert.False(DerivedRunner.CharInSet('a', "", "\x0009"));

            Assert.True(DerivedRunner.CharInSet('4', "xz", "\x0009"));
            Assert.True(DerivedRunner.CharInSet('a', "az", "\x0009"));
            Assert.False(DerivedRunner.CharInSet('a', "xz", "\x0009"));
        }

        private sealed class DerivedRunner : RegexRunner
        {
            public DerivedRunner() { }

            public DerivedRunner(string text)
            {
                runtext = text;
                runtextbeg = 0;
                runtextstart = 0;
                runtextend = text.Length;
                runtextpos = 0;
            }

            public new bool IsBoundary(int index, int startpos, int endpos) => base.IsBoundary(index, startpos, endpos);

            public static new bool CharInSet(char ch, string set, string category) => RegexRunner.CharInSet(ch, set, category);

            protected override bool FindFirstChar() => throw new NotImplementedException();
            protected override void Go() => throw new NotImplementedException();
            protected override void InitTrackCount() => throw new NotImplementedException();
        }

        private static async Task ValidateSetAsync(RegexEngine engine, string regex, RegexOptions options, HashSet<char> included, HashSet<char> excluded, bool validateEveryChar = false)
        {
            Assert.True((included != null) ^ (excluded != null));

            Regex r = await RegexHelpers.GetRegexAsync(engine, regex, options);

            if (validateEveryChar)
            {
                for (int i = 0; i <= char.MaxValue; i++)
                {
                    bool actual = r.IsMatch(((char)i).ToString());
                    bool expected = included != null ? included.Contains((char)i) : !excluded.Contains((char)i);
                    if (actual != expected)
                    {
                        Fail(i);
                    }
                }
            }
            else if (included != null)
            {
                foreach (char c in included)
                {
                    if (!r.IsMatch(c.ToString()))
                    {
                        Fail(c);
                    }
                }
            }
            else
            {
                foreach (char c in excluded)
                {
                    if (r.IsMatch(c.ToString()))
                    {
                        Fail(c);
                    }
                }
            }

            void Fail(int c) => throw new XunitException($"Set=\"{regex}\", Options=\"{options}\", {c:X4} => '{(char)c}'");
        }
    }
}
