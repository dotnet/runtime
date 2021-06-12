// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoCompareTests
    {
        private static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        private static CompareInfo s_currentCompare = CultureInfo.CurrentCulture.CompareInfo;
        private static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        private static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;

        // On Windows, hiragana characters sort after katakana.
        // On ICU, it is the opposite
        private static int s_expectedHiraganaToKatakanaCompare = PlatformDetection.IsNlsGlobalization ? 1 : -1;

        // On Windows, all halfwidth characters sort before fullwidth characters.
        // On ICU, half and fullwidth characters that aren't in the "Halfwidth and fullwidth forms" block U+FF00-U+FFEF
        // sort before the corresponding characters that are in the block U+FF00-U+FFEF
        private static int s_expectedHalfToFullFormsComparison = PlatformDetection.IsNlsGlobalization ? -1 : 1;

        private const string SoftHyphen = "\u00AD";

        public static IEnumerable<object[]> Compare_Kana_TestData()
        {
            CompareOptions ignoreKanaIgnoreWidthIgnoreCase = CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase;
            yield return new object[] { s_invariantCompare, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u3060", "\uFF80\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30C7\u30BF\u30D9\u30B9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E\uFF7D", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30C7", "\uFF83\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30C7\u30BF", "\uFF83\uFF9E\uFF80", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30C7\u30BF\u30D9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\uFF83\uFF9E\uFF70\uFF80\uFF8D\uFF9E\uFF70\uFF7D", "\u3067\u30FC\u305F\u3079\u30FC\u3059", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
            yield return new object[] { s_invariantCompare, "\u3060", "\uFF80\uFF9E", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };

            bool useNls = PlatformDetection.IsNlsGlobalization;

            yield return new object[] { s_invariantCompare, "\u30C7\u30BF\u30D9\u30B9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E\uFF7D", CompareOptions.None, useNls ? 1 : -1 };
            yield return new object[] { s_invariantCompare, "\u30C7", "\uFF83\uFF9E", CompareOptions.None, useNls ? 1 : -1 };
            yield return new object[] { s_invariantCompare, "\u30C7\u30BF", "\uFF83\uFF9E\uFF80", CompareOptions.None, useNls ? 1 : -1 };
            yield return new object[] { s_invariantCompare, "\u30C7\u30BF\u30D9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E", CompareOptions.None, useNls ? 1 : -1 };
            yield return new object[] { s_invariantCompare, "\uFF83\uFF9E\uFF70\uFF80\uFF8D\uFF9E\uFF70\uFF7D", "\u3067\u30FC\u305F\u3079\u30FC\u3059", CompareOptions.None, useNls ? -1 : 1 };
        }

        public static IEnumerable<object[]> Compare_TestData()
        {
            CompareOptions ignoreKanaIgnoreWidthIgnoreCase = CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase;
            yield return new object[] { s_invariantCompare, "\u3042", "\u30A2", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u3042", "\uFF71", ignoreKanaIgnoreWidthIgnoreCase, 0 };

            yield return new object[] { s_invariantCompare, "\u304D\u3083", "\u30AD\u30E3", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u304D\u3083", "\u30AD\u3083", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u304D \u3083", "\u30AD\u3083", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u3044", "I", ignoreKanaIgnoreWidthIgnoreCase, 1 };

            yield return new object[] { s_invariantCompare, "a", "A", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "a", "\uFF41", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "ABCDE", "\uFF21\uFF22\uFF23\uFF24\uFF25", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "ABCDE", "\uFF21\uFF22\uFF23D\uFF25", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "ABCDE", "a\uFF22\uFF23D\uFF25", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "ABCDE", "\uFF41\uFF42\uFF23D\uFF25", ignoreKanaIgnoreWidthIgnoreCase, 0 };

            yield return new object[] { s_invariantCompare, "\u6FA4", "\u6CA2", ignoreKanaIgnoreWidthIgnoreCase, 1 };

            yield return new object[] { s_invariantCompare, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u30D6\u30D9\u30DC", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\u30DC", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3079\uFF8E\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u3071\u3074\u30D7\u307A", "\uFF8B\uFF9F\uFF8C\uFF9F", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3070\uFF8E\uFF9E\u30D6", ignoreKanaIgnoreWidthIgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C\u3079\u307C", "\u3079\uFF8E\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u3070\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", ignoreKanaIgnoreWidthIgnoreCase, -1 };

            yield return new object[] { s_invariantCompare, "ABDDE", "D", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "ABCDE", "\uFF43D", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "ABCDE", "c", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u3060", "\u305F", ignoreKanaIgnoreWidthIgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "\u3060", "\u30C0", ignoreKanaIgnoreWidthIgnoreCase, 0 };

            yield return new object[] { s_invariantCompare, "\u30BF", "\uFF80", ignoreKanaIgnoreWidthIgnoreCase, 0 };

            yield return new object[] { s_invariantCompare, "\u68EE\u9D0E\u5916", "\u68EE\u9DD7\u5916", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u68EE\u9DD7\u5916", "\u68EE\u9DD7\u5916", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u2019\u2019\u2019\u2019", "''''", ignoreKanaIgnoreWidthIgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "\u2019", "'", ignoreKanaIgnoreWidthIgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "", "'", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u4E00", "\uFF11", ignoreKanaIgnoreWidthIgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "\u2160", "\uFF11", ignoreKanaIgnoreWidthIgnoreCase, 1 };

            yield return new object[] { s_invariantCompare, "0", "\uFF10", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "10", "1\uFF10", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "9999\uFF1910", "1\uFF10", ignoreKanaIgnoreWidthIgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "9999\uFF191010", "1\uFF10", ignoreKanaIgnoreWidthIgnoreCase, 1 };

            yield return new object[] { s_invariantCompare, "'\u3000'", "' '", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\uFF1B", ";", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\uFF08", "(", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\uFF70", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\uFF0D", ignoreKanaIgnoreWidthIgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\u30FC", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\u2015", ignoreKanaIgnoreWidthIgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\u2010", ignoreKanaIgnoreWidthIgnoreCase, 1 };

            yield return new object[] { s_invariantCompare, "/", "\uFF0F", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "'", "\uFF07", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\"", "\uFF02", ignoreKanaIgnoreWidthIgnoreCase, 0 };

            yield return new object[] { s_invariantCompare, "\u3042", "\u30A1", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
            yield return new object[] { s_invariantCompare, "\u3042", "\u30A2", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
            yield return new object[] { s_invariantCompare, "\u3042", "\uFF71", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
            yield return new object[] { s_invariantCompare, "\u304D\u3083", "\u30AD\u30E3", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
            yield return new object[] { s_invariantCompare, "\u304D\u3083", "\u30AD\u3083", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };

            yield return new object[] { s_invariantCompare, "\u304D \u3083", "\u30AD\u3083", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\u3044", "I", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "a", "A", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "a", "\uFF41", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "ABCDE", "\uFF21\uFF22\uFF23\uFF24\uFF25", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "ABCDE", "\uFF21\uFF22\uFF23D\uFF25", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5554) + "b", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "ABCDE", "\uFF41\uFF42\uFF23D\uFF25", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u6FA4", "\u6CA2", CompareOptions.None, 1 };

            yield return new object[] { s_invariantCompare, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u30D6\u30D9\u30DC", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
            yield return new object[] { s_invariantCompare, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\u30DC", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
            yield return new object[] { s_invariantCompare, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };

            yield return new object[] { s_invariantCompare, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3079\uFF8E\uFF9E", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\u3071\u3074\u30D7\u307A", "\uFF8B\uFF9F\uFF8C\uFF9F", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3070\uFF8E\uFF9E\u30D6", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C\u3079\u307C", "\u3079\uFF8E\uFF9E", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\u3070\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", CompareOptions.None, -1 };

            yield return new object[] { s_invariantCompare, "ABDDE", "D", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "ABCDE", "\uFF43D\uFF25", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "ABCDE", "\uFF43D", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "ABCDE", "c", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\u3060", "\u305F", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u3060", "\u30C0", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
            yield return new object[] { s_invariantCompare, "\u68EE\u9D0E\u5916", "\u68EE\u9DD7\u5916", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\u68EE\u9DD7\u5916", "\u68EE\u9DD7\u5916", CompareOptions.None, 0 };

            yield return new object[] { s_invariantCompare, "\u2019\u2019\u2019\u2019", "''''", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u2019\u2019\u2019\u2019", "''''", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u2019\u2019\u2019\u2019", "''''", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u2019", "'", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "", "'", CompareOptions.None, -1 };

            yield return new object[] { s_invariantCompare, "\u4E00", "\uFF11", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u2160", "\uFF11", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "0", "\uFF10", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "10", "1\uFF10", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "1\uFF10", "1\uFF10", CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "9999\uFF1910", "1\uFF10", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "9999\uFF191010", "1\uFF10", CompareOptions.None, 1 };

            yield return new object[] { s_invariantCompare, "'\u3000'", "' '", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\uFF1B", ";", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\uFF08", "(", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\uFF0D", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\u30FC", CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\u2015", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\u2010", CompareOptions.None, 1 };

            yield return new object[] { s_invariantCompare, "/", "\uFF0F", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "'", "\uFF07", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\"", "\uFF02", CompareOptions.None, -1 };

            // Hungarian
            yield return new object[] { s_hungarianCompare, "dzsdzs", "ddzs", CompareOptions.Ordinal, 1 };
            yield return new object[] { s_invariantCompare, "dzsdzs", "ddzs", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "dzsdzs", "ddzs", CompareOptions.Ordinal, 1 };

            // Turkish
            yield return new object[] { s_turkishCompare, "i", "I", CompareOptions.None, 1 };
            yield return new object[] { s_turkishCompare, "i", "I", CompareOptions.IgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "i", "\u0130", CompareOptions.None, -1 };
            yield return new object[] { s_turkishCompare, "i", "\u0130", CompareOptions.IgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "i", "I", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "i", "I", CompareOptions.IgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "i", "\u0130", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "i", "\u0130", CompareOptions.IgnoreCase, -1 };

            yield return new object[] { s_invariantCompare, "\u00C0", "A\u0300", CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "\u00C0", "A\u0300", CompareOptions.Ordinal, 1 };
            yield return new object[] { s_invariantCompare, "\u00C0", "a\u0300", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u00C0", "a\u0300", CompareOptions.IgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u00C0", "a\u0300", CompareOptions.Ordinal, 1 };
            yield return new object[] { s_invariantCompare, "\u00C0", "a\u0300", CompareOptions.OrdinalIgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "FooBar", "Foo\u0400Bar", CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "FooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace, 0 };

            yield return new object[] { s_invariantCompare, "Test's", "Tests", CompareOptions.IgnoreSymbols, 0 };
            yield return new object[] { s_invariantCompare, "Test's", "Tests", CompareOptions.StringSort, -1 };

            yield return new object[] { s_invariantCompare, null, "Tests", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Test's", null, CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, null, null, CompareOptions.None, 0 };

            yield return new object[] { s_invariantCompare, "", "Tests", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Tests", "", CompareOptions.None, 1 };

            yield return new object[] { s_invariantCompare, null, "", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "", null, CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "", "", CompareOptions.None, 0 };

            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5555), CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "foobar", "FooB\u00C0R", CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "foobar", "FooB\u00C0R", CompareOptions.IgnoreNonSpace, -1 };

            yield return new object[] { s_invariantCompare, "\uFF9E", "\u3099", CompareOptions.IgnoreNonSpace, 0 };
            yield return new object[] { s_invariantCompare, "\uFF9E", "\u3099", CompareOptions.IgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u20A9", "\uFFE6", CompareOptions.IgnoreWidth, 0 };
            yield return new object[] { s_invariantCompare, "\u20A9", "\uFFE6", CompareOptions.IgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "\u20A9", "\uFFE6", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\u0021", "\uFF01", CompareOptions.IgnoreSymbols, 0 };
            yield return new object[] { s_invariantCompare, "\u00A2", "\uFFE0", CompareOptions.IgnoreSymbols, 0 };
            yield return new object[] { s_invariantCompare, "$", "&", CompareOptions.IgnoreSymbols, 0 };
            yield return new object[] { s_invariantCompare, "\uFF65", "\u30FB", CompareOptions.IgnoreSymbols, 0 };
            yield return new object[] { s_invariantCompare, "\u0021", "\uFF01", CompareOptions.IgnoreWidth, 0 };
            yield return new object[] { s_invariantCompare, "\u0021", "\uFF01", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "\uFF66", "\u30F2", CompareOptions.IgnoreWidth, 0 };

            yield return new object[] { s_invariantCompare, "\uFF66", "\u30F2", CompareOptions.IgnoreSymbols, s_expectedHalfToFullFormsComparison };
            yield return new object[] { s_invariantCompare, "\uFF66", "\u30F2", CompareOptions.IgnoreCase, s_expectedHalfToFullFormsComparison };
            yield return new object[] { s_invariantCompare, "\uFF66", "\u30F2", CompareOptions.IgnoreNonSpace, s_expectedHalfToFullFormsComparison };
            yield return new object[] { s_invariantCompare, "\uFF66", "\u30F2", CompareOptions.None, s_expectedHalfToFullFormsComparison };

            yield return new object[] { s_invariantCompare, "\u3060", "\u30C0", CompareOptions.IgnoreKanaType, 0 };
            yield return new object[] { s_invariantCompare, "\u3060", "\u30C0", CompareOptions.IgnoreCase, s_expectedHiraganaToKatakanaCompare };
            yield return new object[] { s_invariantCompare, "c", "C", CompareOptions.IgnoreKanaType, -1 };

            // Spanish
            yield return new object[] { new CultureInfo("es-ES").CompareInfo, "llegar", "lugar", CompareOptions.None, -1 };

            // Misc differences between platforms
            bool useNls = PlatformDetection.IsNlsGlobalization;

            yield return new object[] { s_invariantCompare, "\u3042", "\u30A1", CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase, useNls ? 1: 0 };
            yield return new object[] { s_invariantCompare, "'\u3000'", "''", CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase, useNls ? 1 : -1 };
            yield return new object[] { s_invariantCompare, "\u30BF", "\uFF80", CompareOptions.None, useNls ? 1 : -1 };
            yield return new object[] { s_invariantCompare, "'\u3000'", "''", CompareOptions.None, useNls ? 1 : -1 };
            yield return new object[] { s_invariantCompare, "\u30FC", "\uFF70", CompareOptions.None, useNls ? 0 : -1 };
            yield return new object[] { s_hungarianCompare, "dzsdzs", "ddzs", CompareOptions.None, useNls ? 0 : -1 };
            yield return new object[] { s_invariantCompare, "Test's", "Tests", CompareOptions.None, useNls ? 1 : -1 };
            yield return new object[] { new CultureInfo("de-DE").CompareInfo, "\u00DC", "UE", CompareOptions.None, -1 };
            if (PlatformDetection.IsNotBrowser)
            {
                yield return new object[] { new CultureInfo("de-DE_phoneb").CompareInfo, "\u00DC", "UE", CompareOptions.None, useNls ? 0 : -1 };
                yield return new object[] { new CultureInfo("es-ES_tradnl").CompareInfo, "llegar", "lugar", CompareOptions.None, useNls ? 1 : -1 };
            }
        }

        // There is a regression in Windows 190xx version with the Kana comparison. Avoid running this test there.
        public static bool IsNotWindowsKanaRegressedVersion() => !PlatformDetection.IsWindows10Version1903OrGreater ||
                                                              PlatformDetection.IsIcuGlobalization ||
                                                              s_invariantCompare.Compare("\u3060", "\uFF80\uFF9E", CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase) == 0;

        [Fact]
        public void CompareWithUnassignedChars()
        {
            int result = PlatformDetection.IsNlsGlobalization ? 0 : -1;
            Compare(s_invariantCompare, "FooBar", "Foo\uFFFFBar", CompareOptions.None, result);
            Compare(s_invariantCompare, "FooBar", "Foo\uFFFFBar", CompareOptions.IgnoreNonSpace, result);
        }

        [ConditionalTheory(nameof(IsNotWindowsKanaRegressedVersion))]
        [MemberData(nameof(Compare_Kana_TestData))]
        public void CompareWithKana(CompareInfo compareInfo, string string1, string string2, CompareOptions options, int expected)
        {
            Compare_Advanced(compareInfo, string1, 0, string1?.Length ?? 0, string2, 0, string2?.Length ?? 0, options, expected);
        }

        [Theory]
        [MemberData(nameof(Compare_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36672", TestPlatforms.Android)]
        public void Compare(CompareInfo compareInfo, string string1, string string2, CompareOptions options, int expected)
        {
            Compare_Advanced(compareInfo, string1, 0, string1?.Length ?? 0, string2, 0, string2?.Length ?? 0, options, expected);
        }

        public static IEnumerable<object[]> Compare_Advanced_TestData()
        {
            yield return new object[] { s_invariantCompare, "Hello", 2, 3, "Hello", 2, 3, CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 2, 2, "ello", 1, 2, CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 1, 0, "Hello", 1, 0, CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 5, 0, "Hello", 0, 0, CompareOptions.None, 0 };

            yield return new object[] { s_invariantCompare, "Hello", 2, 3, "Hemlo", 2, 3, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Hello", 2, 2, "elmo", 1, 2, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Hello", 1, 0, "Goodbye", 1, 0, CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 5, 0, "Goodbye", 0, 0, CompareOptions.None, 0 };

            yield return new object[] { s_invariantCompare, "Hello", 1, 2, "hElLo", 1, 2, CompareOptions.OrdinalIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 1, 2, "heLLo", 1, 3, CompareOptions.OrdinalIgnoreCase, -1 };

            yield return new object[] { s_invariantCompare, null, 0, 0, null, 0, 0, CompareOptions.Ordinal, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 0, 5, null, 0, 0, CompareOptions.Ordinal, 1 };
            yield return new object[] { s_invariantCompare, null, 0, 0, "Hello", 0, 5, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "Hello", 0, 0, "Hello", 0, 0, CompareOptions.Ordinal, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 0, 5, "Hello", 0, 5, CompareOptions.Ordinal, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 0, 3, "Hello", 0, 3, CompareOptions.Ordinal, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 2, 3, "Hello", 2, 3, CompareOptions.Ordinal, 0 };
            yield return new object[] { s_invariantCompare, "Hello", 0, 5, "He" + SoftHyphen + "llo", 0, 5, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "Hello", 0, 5, "-=<Hello>=-", 3, 5, CompareOptions.Ordinal, 0 };
            yield return new object[] { s_invariantCompare, "\uD83D\uDD53Hello\uD83D\uDD50", 1, 7, "\uD83D\uDD53Hello\uD83D\uDD54", 1, 7, CompareOptions.Ordinal, 0 }; // Surrogate split
            yield return new object[] { s_invariantCompare, "Hello", 0, 5, "Hello123", 0, 8, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "Hello123", 0, 8, "Hello", 0, 5, CompareOptions.Ordinal, 1 };
            yield return new object[] { s_invariantCompare, "---aaaaaaaaaaa", 3, 11, "+++aaaaaaaaaaa", 3, 11, CompareOptions.Ordinal, 0 }; // Equal long alignment 2, equal compare
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 3, 11, "aaaxaaaaaaaaaa", 3, 11, CompareOptions.Ordinal, -1 }; // Equal long alignment 2, different compare at n=1
            yield return new object[] { s_invariantCompare, "-aaaaaaaaaaaaa", 1, 13, "+aaaaaaaaaaaaa", 1, 13, CompareOptions.Ordinal, 0 }; // Equal long alignment 6, equal compare
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 1, 13, "axaaaaaaaaaaaa", 1, 13, CompareOptions.Ordinal, -1 }; // Equal long alignment 6, different compare at n=1
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 0, 14, "aaaaaaaaaaaaaa", 0, 14, CompareOptions.Ordinal, 0 }; // Equal long alignment 4, equal compare
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 0, 14, "xaaaaaaaaaaaaa", 0, 14, CompareOptions.Ordinal, -1 }; // Equal long alignment 4, different compare at n=1
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 0, 14, "axaaaaaaaaaaaa", 0, 14, CompareOptions.Ordinal, -1 }; // Equal long alignment 4, different compare at n=2
            yield return new object[] { s_invariantCompare, "--aaaaaaaaaaaa", 2, 12, "++aaaaaaaaaaaa", 2, 12, CompareOptions.Ordinal, 0 }; // Equal long alignment 0, equal compare
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 2, 12, "aaxaaaaaaaaaaa", 2, 12, CompareOptions.Ordinal, -1 }; // Equal long alignment 0, different compare at n=1
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 2, 12, "aaaxaaaaaaaaaa", 2, 12, CompareOptions.Ordinal, -1 }; // Equal long alignment 0, different compare at n=2
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 2, 12, "aaaaxaaaaaaaaa", 2, 12, CompareOptions.Ordinal, -1 }; // Equal long alignment 0, different compare at n=3
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 2, 12, "aaaaaxaaaaaaaa", 2, 12, CompareOptions.Ordinal, -1 }; // Equal long alignment 0, different compare at n=4
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 2, 12, "aaaaaaxaaaaaaa", 2, 12, CompareOptions.Ordinal, -1 }; // Equal long alignment 0, different compare at n=5
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 0, 13, "+aaaaaaaaaaaaa", 1, 13, CompareOptions.Ordinal, 0 }; // Different int alignment, equal compare
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 0, 13, "aaaaaaaaaaaaax", 1, 13, CompareOptions.Ordinal, -1 }; // Different int alignment
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 1, 13, "aaaxaaaaaaaaaa", 3, 11, CompareOptions.Ordinal, -1 }; // Different long alignment, abs of 4, one of them is 2, different at n=1
            yield return new object[] { s_invariantCompare, "-aaaaaaaaaaaaa", 1, 10, "++++aaaaaaaaaa", 4, 10, CompareOptions.Ordinal, 0 }; // Different long alignment, equal compare
            yield return new object[] { s_invariantCompare, "aaaaaaaaaaaaaa", 1, 10, "aaaaaaaaaaaaax", 4, 10, CompareOptions.Ordinal, -1 }; // Different long alignment
        }

        [Theory]
        [MemberData(nameof(Compare_Advanced_TestData))]
        public void Compare_Advanced(CompareInfo compareInfo, string string1, int offset1, int length1, string string2, int offset2, int length2, CompareOptions options, int expected)
        {
            if (offset1 + length1 == (string1?.Length ?? 0) && offset2 + length2 == (string2?.Length ?? 0))
            {
                if (offset1 == 0 && offset2 == 0)
                {
                    if (options == CompareOptions.None)
                    {
                        // Use Compare(string, string)
                        Assert.Equal(expected, Math.Sign(compareInfo.Compare(string1, string2)));
                        Assert.Equal(-expected, Math.Sign(compareInfo.Compare(string2, string1)));
                    }
                    // Use Compare(string, string, CompareOptions)
                    Assert.Equal(expected, Math.Sign(compareInfo.Compare(string1, string2, options)));
                    Assert.Equal(-expected, Math.Sign(compareInfo.Compare(string2, string1, options)));
                }
                if (options == CompareOptions.None)
                {
                    // Use Compare(string, int, string, int)
                    Assert.Equal(expected, Math.Sign(compareInfo.Compare(string1, offset1, string2, offset2)));
                    Assert.Equal(-expected, Math.Sign(compareInfo.Compare(string2, offset2, string1, offset1)));
                }
                // Use Compare(string, int, string, int, CompareOptions)
                Assert.Equal(expected, Math.Sign(compareInfo.Compare(string1, offset1, string2, offset2, options)));
                Assert.Equal(-expected, Math.Sign(compareInfo.Compare(string2, offset2, string1, offset1, options)));
            }
            if (options == CompareOptions.None)
            {
                // Use Compare(string, int, int, string, int, int)
                Assert.Equal(expected, Math.Sign(compareInfo.Compare(string1, offset1, length1, string2, offset2, length2)));
                Assert.Equal(-expected, Math.Sign(compareInfo.Compare(string2, offset2, length2, string1, offset1, length1)));
            }
            // Use Compare(string, int, int, string, int, int, CompareOptions)
            Assert.Equal(expected, Math.Sign(compareInfo.Compare(string1, offset1, length1, string2, offset2, length2, options)));
            Assert.Equal(-expected, Math.Sign(compareInfo.Compare(string2, offset2, length2, string1, offset1, length1, options)));

            // Now test the span-based versions - use BoundedMemory to detect buffer overruns
            // We can't run this test for null inputs since they implicitly convert to empty span

            if (string1 != null && string2 != null)
            {
                RunSpanCompareTest(compareInfo, string1.AsSpan(offset1, length1), string2.AsSpan(offset2, length2), options, expected);
            }

            static void RunSpanCompareTest(CompareInfo compareInfo, ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options, int expected)
            {
                using BoundedMemory<char> string1BoundedMemory = BoundedMemory.AllocateFromExistingData(string1);
                string1BoundedMemory.MakeReadonly();

                using BoundedMemory<char> string2BoundedMemory = BoundedMemory.AllocateFromExistingData(string2);
                string2BoundedMemory.MakeReadonly();

                Assert.Equal(expected, Math.Sign(compareInfo.Compare(string1, string2, options)));
                Assert.Equal(-expected, Math.Sign(compareInfo.Compare(string2, string1, options)));
            }
        }

        [Fact]
        public void Compare_Invalid()
        {
            // Compare options are invalid
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.Compare("Tests", "Tests", (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.Compare("Tests", 0, "Tests", 0, (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.Compare("Tests", 0, 2, "Tests", 0, 2, (CompareOptions)(-1)));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.Compare("Tests", "Tests", CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.Compare("Tests", 0, "Tests", 0, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.Compare("Tests", 0, 2, "Tests", 0, 2, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.Compare("Tests", "Tests", CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.Compare("Tests", 0, "Tests", 0, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.Compare("Tests", 0, 2, "Tests", 0, 2, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));

            // Offset1 < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset1", () => s_invariantCompare.Compare("Test", -1, "Test", 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset1", () => s_invariantCompare.Compare("Test", -1, "Test", 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset1", () => s_invariantCompare.Compare("Test", -1, 2, "Test", 0, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset1", () => s_invariantCompare.Compare("Test", -1, 2, "Test", 0, 2, CompareOptions.None));

            // Offset1 > string1.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length1", () => s_invariantCompare.Compare("Test", 5, "Test", 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length1", () => s_invariantCompare.Compare("Test", 5, "Test", 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string1", () => s_invariantCompare.Compare("Test", 5, 0, "Test", 0, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string1", () => s_invariantCompare.Compare("Test", 5, 0, "Test", 0, 2, CompareOptions.None));

            // Offset2 < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset2", () => s_invariantCompare.Compare("Test", 0, "Test", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset2", () => s_invariantCompare.Compare("Test", 0, "Test", -1, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", -1, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", -1, 2, CompareOptions.None));

            // Offset2 > string2.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length2", () => s_invariantCompare.Compare("Test", 0, "Test", 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length2", () => s_invariantCompare.Compare("Test", 0, "Test", 5, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", 5, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", 5, 0, CompareOptions.None));

            // Length1 < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length1", () => s_invariantCompare.Compare("Test", 0, -1, "Test", 0, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length1", () => s_invariantCompare.Compare("Test", 0, -1, "Test", 0, 2, CompareOptions.None));

            // Length1 > string1.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string1", () => s_invariantCompare.Compare("Test", 0, 5, "Test", 0, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string1", () => s_invariantCompare.Compare("Test", 0, 5, "Test", 0, 2, CompareOptions.None));

            // Length2 < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", 0, -1, CompareOptions.None));

            // Length2 > string2.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", 0, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", 0, 5, CompareOptions.None));

            // Offset1 + length1 > string1.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string1", () => s_invariantCompare.Compare("Test", 2, 3, "Test", 0, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string1", () => s_invariantCompare.Compare("Test", 2, 3, "Test", 0, 2, CompareOptions.None));

            // Offset2 + length2 > string2.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", 2, 3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string2", () => s_invariantCompare.Compare("Test", 0, 2, "Test", 2, 3, CompareOptions.None));
        }

        [Fact]
        public void TestIgnoreKanaAndWidthCases()
        {
            for (char c = '\uFF41'; c <= '\uFF5A'; c++) // Full width 'ａ' to `ｚ`
            {
                Assert.False(string.Equals(new string(c, 1), new string((char) (c - 0x20), 1), StringComparison.InvariantCulture), $"Expected '{(int)c:x4}' != '{c - 0x20:x4}'");
                Assert.True(string.Equals(new string(c, 1), new string((char) (c - 0x20), 1), StringComparison.InvariantCultureIgnoreCase), $"Expected '{(int)c:x4}' == '{c - 0x20:x4}'");
            }

            // Edge case of the Ignore Width.
            Assert.False(string.Compare("\u3162\u3163", "\uFFDB\uFFDC", CultureInfo.InvariantCulture, CompareOptions.None) == 0, $"Expect '0x3162 0x3163' != '0xFFDB 0xFFDC'");
            Assert.True(string.Compare("\u3162\u3163", "\uFFDB\uFFDC", CultureInfo.InvariantCulture, CompareOptions.IgnoreWidth) == 0, "Expect '0x3162 0x3163' == '0xFFDB 0xFFDC'");

            const char hiraganaStart = '\u3041';
            const char hiraganaEnd = '\u3096';
            const int hiraganaToKatakanaOffset = 0x30a1 - 0x3041;

            for (Char hiraganaChar = hiraganaStart; hiraganaChar <= hiraganaEnd; hiraganaChar++)
            {
                Assert.False(string.Compare(new string(hiraganaChar, 1), new string((char)(hiraganaChar + hiraganaToKatakanaOffset), 1), CultureInfo.InvariantCulture, CompareOptions.IgnoreCase) == 0,
                                            $"Expect '{(int)hiraganaChar:x4}'  != {(int)hiraganaChar + hiraganaToKatakanaOffset:x4} with CompareOptions.IgnoreCase");
                Assert.True(string.Compare(new string(hiraganaChar, 1), new string((char)(hiraganaChar + hiraganaToKatakanaOffset), 1), CultureInfo.InvariantCulture, CompareOptions.IgnoreKanaType) == 0,
                                            $"Expect '{(int)hiraganaChar:x4}'  == {(int)hiraganaChar + hiraganaToKatakanaOffset:x4} with CompareOptions.IgnoreKanaType");
            }
        }
    }
}
