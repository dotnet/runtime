// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    [OuterLoop]
    public class RegexCharacterSetTests
    {
        [Theory]
        [InlineData(@"a", RegexOptions.IgnoreCase, new[] { 'a', 'A' })]
        [InlineData(@"ac", RegexOptions.None, new[] { 'a', 'c' })]
        [InlineData(@"ace", RegexOptions.None, new[] { 'a', 'c', 'e' })]
        [InlineData(@"aceg", RegexOptions.None, new[] { 'a', 'c', 'e', 'g' })]
        [InlineData(@"aceg", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'c', 'C', 'e', 'E', 'g', 'G' })]
        [InlineData(@"\u00A9", RegexOptions.None, new[] { '\u00A9' })]
        [InlineData(@"\u00A9", RegexOptions.IgnoreCase, new[] { '\u00A9' })]
        [InlineData(@"\u00FD\u00FF", RegexOptions.None, new[] { '\u00FD', '\u00FF' })]
        [InlineData(@"\u00FE\u0080", RegexOptions.None, new[] { '\u00FE', '\u0080' })]
        [InlineData(@"\u0080\u0082", RegexOptions.None, new[] { '\u0080', '\u0082' })]
        [InlineData(@"az", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z' })]
        [InlineData(@"azY", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y' })]
        [InlineData(@"azY", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y' })]
        [InlineData(@"azY\u00A9", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9' })]
        [InlineData(@"azY\u00A9", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9' })]
        [InlineData(@"azY\u00A9\u05D0", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9', '\u05D0' })]
        [InlineData(@"azY\u00A9\u05D0", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9', '\u05D0' })]
        [InlineData(@"a ", RegexOptions.None, new[] { 'a', ' ' })]
        [InlineData(@"a \t\r", RegexOptions.None, new[] { 'a', ' ', '\t', '\r' })]
        [InlineData(@"aeiou", RegexOptions.None, new[] { 'a', 'e', 'i', 'o', 'u' })]
        [InlineData(@"a-a", RegexOptions.None, new[] { 'a' })]
        [InlineData(@"ab", RegexOptions.None, new[] { 'a', 'b' })]
        [InlineData(@"a-b", RegexOptions.None, new[] { 'a', 'b' })]
        [InlineData(@"abc", RegexOptions.None, new[] { 'a', 'b', 'c' })]
        [InlineData(@"1369", RegexOptions.None, new[] { '1', '3', '6', '9' })]
        [InlineData(@"ACEGIKMOQSUWY", RegexOptions.None, new[] { 'A', 'C', 'E', 'G', 'I', 'K', 'M', 'O', 'Q', 'S', 'U', 'W', 'Y' })]
        [InlineData(@"abcAB", RegexOptions.None, new[] { 'A', 'B', 'a', 'b', 'c' })]
        [InlineData(@"a-c", RegexOptions.None, new[] { 'a', 'b', 'c' })]
        [InlineData(@"a-fA-F0-9", RegexOptions.None, new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' })]
        [InlineData(@"X-b", RegexOptions.None, new[] { 'X', 'Y', 'Z', '[', '\\', ']', '^', '_', '`', 'a', 'b' })]
        [InlineData(@"\u0083\u00DE-\u00E1", RegexOptions.None, new[] { '\u0083', '\u00DE', '\u00DF', '\u00E0', '\u00E1' })]
        [InlineData(@"\u007A-\u0083\u00DE-\u00E1", RegexOptions.None, new[] { '\u007A', '\u007B', '\u007C', '\u007D', '\u007E', '\u007F', '\u0080', '\u0081', '\u0082', '\u0083', '\u00DE', '\u00DF', '\u00E0', '\u00E1' })]
        [InlineData(@"\u05D0", RegexOptions.None, new[] { '\u05D0' })]
        [InlineData(@"a\u05D0", RegexOptions.None, new[] { 'a', '\u05D0' })]
        [InlineData(@"\uFFFC-\uFFFF", RegexOptions.None, new[] { '\uFFFC', '\uFFFD', '\uFFFE', '\uFFFF' })]
        [InlineData(@"[a-z-[d-w-[m-o]]]", RegexOptions.None, new[] { 'a', 'b', 'c', 'm', 'n', 'n', 'o', 'x', 'y', 'z' })]
        [InlineData(@"\p{IsBasicLatin}-[\x00-\x7F]", RegexOptions.None, new char[0])]
        [InlineData(@"[0-9-[2468]]", RegexOptions.None, new[] { '0', '1', '3', '5', '7', '9' })]
        public void SetInclusionsExpected(string set, RegexOptions options, char[] expectedIncluded)
        {
            bool hasBracket = set.Contains("[");
            if (hasBracket)
            {
                ValidateSet(set, options, new HashSet<char>(expectedIncluded), null, validateEveryChar: true);
            }
            else
            {
                ValidateSet($"[{set}]", options, new HashSet<char>(expectedIncluded), null);
                ValidateSet($"[^{set}]", options, null, new HashSet<char>(expectedIncluded));
            }
        }

        [Theory]
        [InlineData(@"[^1234-[3456]]", RegexOptions.None, new[] { '1', '2', '3', '4', '5', '6' })]
        public void SetExclusionsExpected(string set, RegexOptions options, char[] expectedExcluded)
        {
            ValidateSet(set, options, null, new HashSet<char>(expectedExcluded), validateEveryChar: true);
        }

        [Theory]
        [InlineData('\0')]
        [InlineData('\uFFFF')]
        [InlineData('a')]
        [InlineData('5')]
        [InlineData('\u00FF')]
        [InlineData('\u0080')]
        [InlineData('\u0100')]
        public void SingleExpected(char c)
        {
            string s = $@"\u{(int)c:X4}";
            var set = new HashSet<char>() { c };

            // One
            ValidateSet($"{s}", RegexOptions.None, set, null);
            ValidateSet($"[{s}]", RegexOptions.None, set, null);
            ValidateSet($"[^{s}]", RegexOptions.None, null, set);

            // Positive lookahead
            ValidateSet($"(?={s}){s}", RegexOptions.None, set, null);
            ValidateSet($"(?=[^{s}])[^{s}]", RegexOptions.None, null, set);

            // Negative lookahead
            ValidateSet($"(?![^{s}]){s}", RegexOptions.None, set, null);
            ValidateSet($"(?![{s}])[^{s}]", RegexOptions.None, null, set);

            // Concatenation
            ValidateSet($"[{s}{s}]", RegexOptions.None, set, null);
            ValidateSet($"[^{s}{s}{s}]", RegexOptions.None, null, set);

            // Alternation
            ValidateSet($"{s}|{s}", RegexOptions.None, set, null);
            ValidateSet($"[^{s}]|[^{s}]|[^{s}]", RegexOptions.None, null, set);
            ValidateSet($"{s}|[^{s}]", RegexOptions.None, null, new HashSet<char>());
        }

        [Fact]
        public void AllEmptySets()
        {
            var set = new HashSet<char>();

            ValidateSet(@"[\u0000-\uFFFF]", RegexOptions.None, null, set);
            ValidateSet(@"[\u0000-\uFFFFa-z]", RegexOptions.None, null, set);
            ValidateSet(@"[\u0000-\u1000\u1001-\u2002\u2003-\uFFFF]", RegexOptions.None, null, set);
            ValidateSet(@"[\u0000-\uFFFE\u0001-\uFFFF]", RegexOptions.None, null, set, validateEveryChar: true);

            ValidateSet(@"[^\u0000-\uFFFF]", RegexOptions.None, set, null);
            ValidateSet(@"[^\u0000-\uFFFFa-z]", RegexOptions.None, set, null);
            ValidateSet(@"[^\u0000-\uFFFE\u0001-\uFFFF]", RegexOptions.None, set, null);
            ValidateSet(@"[^\u0000-\u1000\u1001-\u2002\u2003-\uFFFF]", RegexOptions.None, set, null, validateEveryChar: true);
        }

        [Fact]
        public void AllButOneSets()
        {
            ValidateSet(@"[\u0000-\uFFFE]", RegexOptions.None, null, new HashSet<char>() { '\uFFFF' });
            ValidateSet(@"[\u0001-\uFFFF]", RegexOptions.None, null, new HashSet<char>() { '\u0000' });
            ValidateSet(@"[\u0000-ac-\uFFFF]", RegexOptions.None, null, new HashSet<char>() { 'b' }, validateEveryChar: true);
        }

        [Fact]
        public void DotInclusionsExpected()
        {
            ValidateSet(".", RegexOptions.None, null, new HashSet<char>() { '\n' });
            ValidateSet(".", RegexOptions.IgnoreCase, null, new HashSet<char>() { '\n' });
            ValidateSet(".", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, null, new HashSet<char>() { '\n' }, validateEveryChar: true);

            ValidateSet(".", RegexOptions.Singleline, null, new HashSet<char>());
            ValidateSet(".", RegexOptions.Singleline | RegexOptions.IgnoreCase, null, new HashSet<char>());
            ValidateSet(".", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, null, new HashSet<char>(), validateEveryChar: true);
        }

        [Fact]
        public void WhitespaceInclusionsExpected()
        {
            var whitespaceInclusions = ComputeIncludedSet(char.IsWhiteSpace);
            ValidateSet(@"[\s]", RegexOptions.None, whitespaceInclusions, null);
            ValidateSet(@"[^\s]", RegexOptions.None, null, whitespaceInclusions);
            ValidateSet(@"[\S]", RegexOptions.None, null, whitespaceInclusions);
        }

        [Fact]
        public void DigitInclusionsExpected()
        {
            var digitInclusions = ComputeIncludedSet(char.IsDigit);
            ValidateSet(@"[\d]", RegexOptions.None, digitInclusions, null);
            ValidateSet(@"[^\d]", RegexOptions.None, null, digitInclusions);
            ValidateSet(@"[\D]", RegexOptions.None, null, digitInclusions);
        }

        [Theory]
        [InlineData(@"\p{Lu}", new[] { UnicodeCategory.UppercaseLetter })]
        [InlineData(@"\p{S}", new[] { UnicodeCategory.CurrencySymbol, UnicodeCategory.MathSymbol, UnicodeCategory.ModifierSymbol, UnicodeCategory.OtherSymbol })]
        [InlineData(@"\p{Lu}\p{Zl}", new[] { UnicodeCategory.UppercaseLetter, UnicodeCategory.LineSeparator })]
        [InlineData(@"\w", new[] { UnicodeCategory.LowercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.OtherLetter, UnicodeCategory.ModifierLetter, UnicodeCategory.NonSpacingMark, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.ConnectorPunctuation })]
        public void UnicodeCategoryInclusionsExpected(string set, UnicodeCategory[] categories)
        {
            var categoryInclusions = ComputeIncludedSet(c => Array.IndexOf(categories, char.GetUnicodeCategory(c)) >= 0);
            ValidateSet($"[{set}]", RegexOptions.None, categoryInclusions, null);
            ValidateSet($"[^{set}]", RegexOptions.None, null, categoryInclusions);
        }

        [Theory]
        [InlineData(@"\p{IsBasicLatin}", new[] { 0x0000, 0x007F })]
        [InlineData(@"\p{IsLatin-1Supplement}", new[] { 0x0080, 0x00FF })]
        [InlineData(@"\p{IsLatinExtended-A}", new[] { 0x0100, 0x017F })]
        [InlineData(@"\p{IsLatinExtended-B}", new[] { 0x0180, 0x024F })]
        [InlineData(@"\p{IsIPAExtensions}", new[] { 0x0250, 0x02AF })]
        [InlineData(@"\p{IsSpacingModifierLetters}", new[] { 0x02B0, 0x02FF })]
        [InlineData(@"\p{IsCombiningDiacriticalMarks}", new[] { 0x0300, 0x036F })]
        [InlineData(@"\p{IsGreek}", new[] { 0x0370, 0x03FF })]
        [InlineData(@"\p{IsCyrillic}", new[] { 0x0400, 0x04FF })]
        [InlineData(@"\p{IsCyrillicSupplement}", new[] { 0x0500, 0x052F })]
        [InlineData(@"\p{IsArmenian}", new[] { 0x0530, 0x058F })]
        [InlineData(@"\p{IsHebrew}", new[] { 0x0590, 0x05FF })]
        [InlineData(@"\p{IsArabic}", new[] { 0x0600, 0x06FF })]
        [InlineData(@"\p{IsSyriac}", new[] { 0x0700, 0x074F })]
        [InlineData(@"\p{IsThaana}", new[] { 0x0780, 0x07BF })]
        [InlineData(@"\p{IsDevanagari}", new[] { 0x0900, 0x097F })]
        [InlineData(@"\p{IsBengali}", new[] { 0x0980, 0x09FF })]
        [InlineData(@"\p{IsGurmukhi}", new[] { 0x0A00, 0x0A7F })]
        [InlineData(@"\p{IsGujarati}", new[] { 0x0A80, 0x0AFF })]
        [InlineData(@"\p{IsOriya}", new[] { 0x0B00, 0x0B7F })]
        [InlineData(@"\p{IsTamil}", new[] { 0x0B80, 0x0BFF })]
        [InlineData(@"\p{IsTelugu}", new[] { 0x0C00, 0x0C7F })]
        [InlineData(@"\p{IsKannada}", new[] { 0x0C80, 0x0CFF })]
        [InlineData(@"\p{IsMalayalam}", new[] { 0x0D00, 0x0D7F })]
        [InlineData(@"\p{IsSinhala}", new[] { 0x0D80, 0x0DFF })]
        [InlineData(@"\p{IsThai}", new[] { 0x0E00, 0x0E7F })]
        [InlineData(@"\p{IsLao}", new[] { 0x0E80, 0x0EFF })]
        [InlineData(@"\p{IsTibetan}", new[] { 0x0F00, 0x0FFF })]
        [InlineData(@"\p{IsMyanmar}", new[] { 0x1000, 0x109F })]
        [InlineData(@"\p{IsGeorgian}", new[] { 0x10A0, 0x10FF })]
        [InlineData(@"\p{IsHangulJamo}", new[] { 0x1100, 0x11FF })]
        [InlineData(@"\p{IsEthiopic}", new[] { 0x1200, 0x137F })]
        [InlineData(@"\p{IsCherokee}", new[] { 0x13A0, 0x13FF })]
        [InlineData(@"\p{IsUnifiedCanadianAboriginalSyllabics}", new[] { 0x1400, 0x167F })]
        [InlineData(@"\p{IsOgham}", new[] { 0x1680, 0x169F })]
        [InlineData(@"\p{IsRunic}", new[] { 0x16A0, 0x16FF })]
        [InlineData(@"\p{IsTagalog}", new[] { 0x1700, 0x171F })]
        [InlineData(@"\p{IsHanunoo}", new[] { 0x1720, 0x173F })]
        [InlineData(@"\p{IsBuhid}", new[] { 0x1740, 0x175F })]
        [InlineData(@"\p{IsTagbanwa}", new[] { 0x1760, 0x177F })]
        [InlineData(@"\p{IsKhmer}", new[] { 0x1780, 0x17FF })]
        [InlineData(@"\p{IsMongolian}", new[] { 0x1800, 0x18AF })]
        [InlineData(@"\p{IsLimbu}", new[] { 0x1900, 0x194F })]
        [InlineData(@"\p{IsTaiLe}", new[] { 0x1950, 0x197F })]
        [InlineData(@"\p{IsKhmerSymbols}", new[] { 0x19E0, 0x19FF })]
        [InlineData(@"\p{IsPhoneticExtensions}", new[] { 0x1D00, 0x1D7F })]
        [InlineData(@"\p{IsLatinExtendedAdditional}", new[] { 0x1E00, 0x1EFF })]
        [InlineData(@"\p{IsGreekExtended}", new[] { 0x1F00, 0x1FFF })]
        [InlineData(@"\p{IsGeneralPunctuation}", new[] { 0x2000, 0x206F })]
        [InlineData(@"\p{IsSuperscriptsandSubscripts}", new[] { 0x2070, 0x209F })]
        [InlineData(@"\p{IsCurrencySymbols}", new[] { 0x20A0, 0x20CF })]
        [InlineData(@"\p{IsCombiningDiacriticalMarksforSymbols}", new[] { 0x20D0, 0x20FF })]
        [InlineData(@"\p{IsLetterlikeSymbols}", new[] { 0x2100, 0x214F })]
        [InlineData(@"\p{IsNumberForms}", new[] { 0x2150, 0x218F })]
        [InlineData(@"\p{IsArrows}", new[] { 0x2190, 0x21FF })]
        [InlineData(@"\p{IsMathematicalOperators}", new[] { 0x2200, 0x22FF })]
        [InlineData(@"\p{IsMiscellaneousTechnical}", new[] { 0x2300, 0x23FF })]
        [InlineData(@"\p{IsControlPictures}", new[] { 0x2400, 0x243F })]
        [InlineData(@"\p{IsOpticalCharacterRecognition}", new[] { 0x2440, 0x245F })]
        [InlineData(@"\p{IsEnclosedAlphanumerics}", new[] { 0x2460, 0x24FF })]
        [InlineData(@"\p{IsBoxDrawing}", new[] { 0x2500, 0x257F })]
        [InlineData(@"\p{IsBlockElements}", new[] { 0x2580, 0x259F })]
        [InlineData(@"\p{IsGeometricShapes}", new[] { 0x25A0, 0x25FF })]
        [InlineData(@"\p{IsMiscellaneousSymbols}", new[] { 0x2600, 0x26FF })]
        [InlineData(@"\p{IsDingbats}", new[] { 0x2700, 0x27BF })]
        [InlineData(@"\p{IsMiscellaneousMathematicalSymbols-A}", new[] { 0x27C0, 0x27EF })]
        [InlineData(@"\p{IsSupplementalArrows-A}", new[] { 0x27F0, 0x27FF })]
        [InlineData(@"\p{IsBraillePatterns}", new[] { 0x2800, 0x28FF })]
        [InlineData(@"\p{IsSupplementalArrows-B}", new[] { 0x2900, 0x297F })]
        [InlineData(@"\p{IsMiscellaneousMathematicalSymbols-B}", new[] { 0x2980, 0x29FF })]
        [InlineData(@"\p{IsSupplementalMathematicalOperators}", new[] { 0x2A00, 0x2AFF })]
        [InlineData(@"\p{IsMiscellaneousSymbolsandArrows}", new[] { 0x2B00, 0x2BFF })]
        [InlineData(@"\p{IsCJKRadicalsSupplement}", new[] { 0x2E80, 0x2EFF })]
        [InlineData(@"\p{IsKangxiRadicals}", new[] { 0x2F00, 0x2FDF })]
        [InlineData(@"\p{IsIdeographicDescriptionCharacters}", new[] { 0x2FF0, 0x2FFF })]
        [InlineData(@"\p{IsCJKSymbolsandPunctuation}", new[] { 0x3000, 0x303F })]
        [InlineData(@"\p{IsHiragana}", new[] { 0x3040, 0x309F })]
        [InlineData(@"\p{IsKatakana}", new[] { 0x30A0, 0x30FF })]
        [InlineData(@"\p{IsBopomofo}", new[] { 0x3100, 0x312F })]
        [InlineData(@"\p{IsHangulCompatibilityJamo}", new[] { 0x3130, 0x318F })]
        [InlineData(@"\p{IsKanbun}", new[] { 0x3190, 0x319F })]
        [InlineData(@"\p{IsBopomofoExtended}", new[] { 0x31A0, 0x31BF })]
        [InlineData(@"\p{IsKatakanaPhoneticExtensions}", new[] { 0x31F0, 0x31FF })]
        [InlineData(@"\p{IsEnclosedCJKLettersandMonths}", new[] { 0x3200, 0x32FF })]
        [InlineData(@"\p{IsCJKCompatibility}", new[] { 0x3300, 0x33FF })]
        [InlineData(@"\p{IsCJKUnifiedIdeographsExtensionA}", new[] { 0x3400, 0x4DBF })]
        [InlineData(@"\p{IsYijingHexagramSymbols}", new[] { 0x4DC0, 0x4DFF })]
        [InlineData(@"\p{IsCJKUnifiedIdeographs}", new[] { 0x4E00, 0x9FFF })]
        [InlineData(@"\p{IsYiSyllables}", new[] { 0xA000, 0xA48F })]
        [InlineData(@"\p{IsYiRadicals}", new[] { 0xA490, 0xA4CF })]
        [InlineData(@"\p{IsHangulSyllables}", new[] { 0xAC00, 0xD7AF })]
        [InlineData(@"\p{IsHighSurrogates}", new[] { 0xD800, 0xDB7F })]
        [InlineData(@"\p{IsHighPrivateUseSurrogates}", new[] { 0xDB80, 0xDBFF })]
        [InlineData(@"\p{IsLowSurrogates}", new[] { 0xDC00, 0xDFFF })]
        [InlineData(@"\p{IsPrivateUse}", new[] { 0xE000, 0xF8FF })]
        [InlineData(@"\p{IsCJKCompatibilityIdeographs}", new[] { 0xF900, 0xFAFF })]
        [InlineData(@"\p{IsAlphabeticPresentationForms}", new[] { 0xFB00, 0xFB4F })]
        [InlineData(@"\p{IsArabicPresentationForms-A}", new[] { 0xFB50, 0xFDFF })]
        [InlineData(@"\p{IsVariationSelectors}", new[] { 0xFE00, 0xFE0F })]
        [InlineData(@"\p{IsCombiningHalfMarks}", new[] { 0xFE20, 0xFE2F })]
        [InlineData(@"\p{IsCJKCompatibilityForms}", new[] { 0xFE30, 0xFE4F })]
        [InlineData(@"\p{IsSmallFormVariants}", new[] { 0xFE50, 0xFE6F })]
        [InlineData(@"\p{IsArabicPresentationForms-B}", new[] { 0xFE70, 0xFEFF })]
        [InlineData(@"\p{IsHalfwidthandFullwidthForms}", new[] { 0xFF00, 0xFFEF })]
        [InlineData(@"\p{IsSpecials}", new[] { 0xFFF0, 0xFFFF })]
        [InlineData(@"\p{IsRunic}\p{IsHebrew}", new[] { 0x0590, 0x05FF, 0x16A0, 0x16FF })]
        [InlineData(@"abx-z\p{IsRunic}\p{IsHebrew}", new[] { 0x0590, 0x05FF, 0x16A0, 0x16FF, 'a', 'a', 'b', 'b', 'x', 'x', 'y', 'z' })]
        public void NamedBlocksInclusionsExpected(string set, int[] ranges)
        {
            var included = new HashSet<char>();
            for (int i = 0; i < ranges.Length - 1; i += 2)
            {
                ComputeIncludedSet(c => c >= ranges[i] && c <= ranges[i + 1], included);
            }

            ValidateSet($"[{set}]", RegexOptions.None, included, null);
            ValidateSet($"[^{set}]", RegexOptions.None, null, included);
        }

        [Theory]
        [InlineData("Cc", UnicodeCategory.Control)]
        [InlineData("Cf", UnicodeCategory.Format)]
        [InlineData("Cn", UnicodeCategory.OtherNotAssigned)]
        [InlineData("Co", UnicodeCategory.PrivateUse)]
        [InlineData("Cs", UnicodeCategory.Surrogate)]
        [InlineData("Ll", UnicodeCategory.LowercaseLetter)]
        [InlineData("Lm", UnicodeCategory.ModifierLetter)]
        [InlineData("Lo", UnicodeCategory.OtherLetter)]
        [InlineData("Lt", UnicodeCategory.TitlecaseLetter)]
        [InlineData("Lu", UnicodeCategory.UppercaseLetter)]
        [InlineData("Mc", UnicodeCategory.SpacingCombiningMark)]
        [InlineData("Me", UnicodeCategory.EnclosingMark)]
        [InlineData("Mn", UnicodeCategory.NonSpacingMark)]
        [InlineData("Nd", UnicodeCategory.DecimalDigitNumber)]
        [InlineData("Nl", UnicodeCategory.LetterNumber)]
        [InlineData("No", UnicodeCategory.OtherNumber)]
        [InlineData("Pc", UnicodeCategory.ConnectorPunctuation)]
        [InlineData("Pd", UnicodeCategory.DashPunctuation)]
        [InlineData("Pe", UnicodeCategory.ClosePunctuation)]
        [InlineData("Po", UnicodeCategory.OtherPunctuation)]
        [InlineData("Ps", UnicodeCategory.OpenPunctuation)]
        [InlineData("Pf", UnicodeCategory.FinalQuotePunctuation)]
        [InlineData("Pi", UnicodeCategory.InitialQuotePunctuation)]
        [InlineData("Sc", UnicodeCategory.CurrencySymbol)]
        [InlineData("Sk", UnicodeCategory.ModifierSymbol)]
        [InlineData("Sm", UnicodeCategory.MathSymbol)]
        [InlineData("So", UnicodeCategory.OtherSymbol)]
        [InlineData("Zl", UnicodeCategory.LineSeparator)]
        [InlineData("Zp", UnicodeCategory.ParagraphSeparator)]
        [InlineData("Zs", UnicodeCategory.SpaceSeparator)]
        public void UnicodeCategoriesInclusionsExpected(string generalCategory, UnicodeCategory unicodeCategory)
        {
            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.Compiled })
            {
                Regex r;
                char[] allChars = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).ToArray();
                int expectedInCategory = allChars.Count(c => char.GetUnicodeCategory(c) == unicodeCategory);
                int expectedNotInCategory = allChars.Length - expectedInCategory;

                r = new Regex(@$"\p{{{generalCategory}}}");
                Assert.Equal(expectedInCategory, r.Matches(string.Concat(allChars)).Count);

                r = new Regex(@$"\P{{{generalCategory}}}");
                Assert.Equal(expectedNotInCategory, r.Matches(string.Concat(allChars)).Count);
            }
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

        [Fact]
        public void ValidateValidateSet()
        {
            Assert.Throws<XunitException>(() => ValidateSet("[a]", RegexOptions.None, new HashSet<char>() { 'b' }, null));
            Assert.Throws<XunitException>(() => ValidateSet("[a]", RegexOptions.None, new HashSet<char>() { 'b' }, null, validateEveryChar: true));

            Assert.Throws<XunitException>(() => ValidateSet("[b]", RegexOptions.None, null, new HashSet<char>() { 'b' }));
            Assert.Throws<XunitException>(() => ValidateSet("[b]", RegexOptions.None, null, new HashSet<char>() { 'b' }, validateEveryChar: true));
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

        private static void ValidateSet(string regex, RegexOptions options, HashSet<char> included, HashSet<char> excluded, bool validateEveryChar = false)
        {
            Assert.True((included != null) ^ (excluded != null));

            foreach (RegexOptions compiled in new[] { RegexOptions.None, RegexOptions.Compiled })
            {
                var r = new Regex(regex, options | compiled);

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
            }

            void Fail(int c) => throw new XunitException($"Set=\"{regex}\", Options=\"{options}\", {c:X4} => '{(char)c}'");
        }
    }
}
