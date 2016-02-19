// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;

using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoCompareTests
    {
        private static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        private static CompareInfo s_currentCompare = CultureInfo.CurrentCulture.CompareInfo;
        private static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        private static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;

        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        private static bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // On Windows, hiragana characters sort after katakana.
        // On ICU, it is the opposite
        private static int s_expectedHiraganaToKatakanaCompare = s_isWindows ? 1 : -1;

        // On Windows, all halfwidth characters sort before fullwidth characters.
        // On ICU, half and fullwidth characters that aren't in the "Halfwidth and fullwidth forms" block U+FF00-U+FFEF
        // sort before the corresponding characters that are in the block U+FF00-U+FFEF
        private static int s_expectedHalfToFullFormsComparison = s_isWindows ? -1 : 1;

        private const string SoftHyphen = "\u00AD";
        
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
            yield return new object[] { s_invariantCompare, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
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
            yield return new object[] { s_invariantCompare, "\u3060", "\uFF80\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u3060", "\u30C0", ignoreKanaIgnoreWidthIgnoreCase, 0 };

            yield return new object[] { s_invariantCompare, "\u30C7\u30BF\u30D9\u30B9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E\uFF7D", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30C7", "\uFF83\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30C7\u30BF", "\uFF83\uFF9E\uFF80", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30C7\u30BF\u30D9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "\u30BF", "\uFF80", ignoreKanaIgnoreWidthIgnoreCase, 0 };

            yield return new object[] { s_invariantCompare, "\uFF83\uFF9E\uFF70\uFF80\uFF8D\uFF9E\uFF70\uFF7D", "\u3067\u30FC\u305F\u3079\u30FC\u3059", ignoreKanaIgnoreWidthIgnoreCase, 0 };
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
            yield return new object[] { s_invariantCompare, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
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
            yield return new object[] { s_invariantCompare, "\u3060", "\uFF80\uFF9E", CompareOptions.None, s_expectedHiraganaToKatakanaCompare };
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
        }

        public static IEnumerable<object[]> Compare_Random_TestData()
        {
            string[] interestingStrings = new string[] { null, "", "a", "1", "-", "A", "!", "abc", "aBc", "a\u0400Bc", "I", "i", "\u0130", "\u0131", "A", "\uFF21", "\uFE57" };
            foreach (string string1 in interestingStrings)
            {
                foreach (string string2 in interestingStrings)
                {
                    yield return new object[] { s_currentCompare, string1, string2, CompareOptions.Ordinal, PredictCompareOrdinalResult(string1, string2) };
                }
            }

            for (int i = 0; i < 40; i++)
            {
                char c = s_randomDataGenerator.GetChar(-55);
                yield return new object[] { s_currentCompare, new string(c, 1), new string(c, 1), CompareOptions.Ordinal, 0 };
                for (int j = 0; j < c; j++)
                {
                    yield return new object[] { s_currentCompare, new string(c, 1), new string((char)j, 1), CompareOptions.Ordinal, 1 };
                }
            }

            for (int i = 0; i < 1000; i++)
            {
                string string1 = s_randomDataGenerator.GetString(-55, false, 5, 20);
                string string2 = s_randomDataGenerator.GetString(-55, false, 5, 20);
                yield return new object[] { s_currentCompare, string1, string1, CompareOptions.Ordinal, 0 };
                yield return new object[] { s_currentCompare, string2, string2, CompareOptions.Ordinal, 0 };
                yield return new object[] { s_currentCompare, string1, string2, CompareOptions.Ordinal, PredictCompareOrdinalResult(string1, string2) };
            }
        }
        
        [Theory]
        [MemberData("Compare_TestData")]
        public void Compare(CompareInfo compareInfo, string string1, string string2, CompareOptions options, int expected)
        {
            if (options == CompareOptions.None)
            {
                // Use Compare(string, string)
                Assert.Equal(expected, NormalizeCompare(compareInfo.Compare(string1, string2)));
                Assert.Equal(-expected, NormalizeCompare(compareInfo.Compare(string2, string1)));
            }
            // Use Compare(string, string, CompareOptions)
            Assert.Equal(expected, NormalizeCompare(compareInfo.Compare(string1, string2, options)));
            Assert.Equal(-expected, NormalizeCompare(compareInfo.Compare(string2, string1, options)));
        }

        [Fact]
        [ActiveIssue(5436, PlatformID.AnyUnix)]
        public void Compare_Issue5463()
        {
            // TODO: Remove this function, and combine into Compare_TestData once 5463 is fixed
            Compare(s_invariantCompare, "\u3042", "\u30A1", CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase, 1);
            Compare(s_invariantCompare, "'\u3000'", "''", CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase, 1);
            Compare(s_invariantCompare, "\u30C7\u30BF\u30D9\u30B9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E\uFF7D", CompareOptions.None, 1);
            Compare(s_invariantCompare, "\u30C7", "\uFF83\uFF9E", CompareOptions.None, 1);
            Compare(s_invariantCompare, "\u30C7\u30BF", "\uFF83\uFF9E\uFF80", CompareOptions.None, 1);
            Compare(s_invariantCompare, "\u30C7\u30BF\u30D9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E", CompareOptions.None, 1);
            Compare(s_invariantCompare, "\u30BF", "\uFF80", CompareOptions.None, 1);
            Compare(s_invariantCompare, "\uFF83\uFF9E\uFF70\uFF80\uFF8D\uFF9E\uFF70\uFF7D", "\u3067\u30FC\u305F\u3079\u30FC\u3059", CompareOptions.None, -1);
            Compare(s_invariantCompare, "'\u3000'", "''", CompareOptions.None, 1);
            Compare(s_invariantCompare, "\u30FC", "\uFF70", CompareOptions.None, 0);
            Compare(s_hungarianCompare, "dzsdzs", "ddzs", CompareOptions.None, 0);
            Compare(s_invariantCompare, "FooBar", "Foo" + UnassignedUnicodeCharacter() + "Bar", CompareOptions.None, 0);
            Compare(s_invariantCompare, "FooBar", "Foo" + UnassignedUnicodeCharacter() + "Bar", CompareOptions.IgnoreNonSpace, 0);
            Compare(s_invariantCompare, "Test's", "Tests", CompareOptions.None, 1);
        }

        [Theory]
        [InlineData(null, 0, 0, null, 0, 0, 0)]
        [InlineData("Hello", 0, 5, null, 0, 0, 1)]
        [InlineData(null, 0, 0, "Hello", 0, 5, -1)]
        [InlineData("Hello", 0, 0, "Hello", 0, 0, 0)]
        [InlineData("Hello", 0, 5, "Hello", 0, 5, 0)]
        [InlineData("Hello", 0, 3, "Hello", 0, 3, 0)]
        [InlineData("Hello", 2, 3, "Hello", 2, 3, 0)]
        [InlineData("Hello", 0, 5, "He" + SoftHyphen + "llo", 0, 5, -1)]
        [InlineData("Hello", 0, 5, "-=<Hello>=-", 3, 5, 0)]
        [InlineData("\uD83D\uDD53Hello\uD83D\uDD50", 1, 7, "\uD83D\uDD53Hello\uD83D\uDD54", 1, 7, 0)] // Surrogate split
        [InlineData("Hello", 0, 5, "Hello123", 0, 8, -1)]
        [InlineData("Hello123", 0, 8, "Hello", 0, 5, 1)]
        [InlineData("---aaaaaaaaaaa", 3, 11, "+++aaaaaaaaaaa", 3, 11, 0)]      // Equal long alignment 2, equal compare
        [InlineData("aaaaaaaaaaaaaa", 3, 11, "aaaxaaaaaaaaaa", 3, 11, -1)]     // Equal long alignment 2, different compare at n=1
        [InlineData("-aaaaaaaaaaaaa", 1, 13, "+aaaaaaaaaaaaa", 1, 13, 0)]      // Equal long alignment 6, equal compare
        [InlineData("aaaaaaaaaaaaaa", 1, 13, "axaaaaaaaaaaaa", 1, 13, -1)]     // Equal long alignment 6, different compare at n=1
        [InlineData("aaaaaaaaaaaaaa", 0, 14, "aaaaaaaaaaaaaa", 0, 14, 0)]      // Equal long alignment 4, equal compare
        [InlineData("aaaaaaaaaaaaaa", 0, 14, "xaaaaaaaaaaaaa", 0, 14, -1)]     // Equal long alignment 4, different compare at n=1
        [InlineData("aaaaaaaaaaaaaa", 0, 14, "axaaaaaaaaaaaa", 0, 14, -1)]     // Equal long alignment 4, different compare at n=2
        [InlineData("--aaaaaaaaaaaa", 2, 12, "++aaaaaaaaaaaa", 2, 12, 0)]      // Equal long alignment 0, equal compare
        [InlineData("aaaaaaaaaaaaaa", 2, 12, "aaxaaaaaaaaaaa", 2, 12, -1)]     // Equal long alignment 0, different compare at n=1
        [InlineData("aaaaaaaaaaaaaa", 2, 12, "aaaxaaaaaaaaaa", 2, 12, -1)]     // Equal long alignment 0, different compare at n=2
        [InlineData("aaaaaaaaaaaaaa", 2, 12, "aaaaxaaaaaaaaa", 2, 12, -1)]     // Equal long alignment 0, different compare at n=3
        [InlineData("aaaaaaaaaaaaaa", 2, 12, "aaaaaxaaaaaaaa", 2, 12, -1)]     // Equal long alignment 0, different compare at n=4
        [InlineData("aaaaaaaaaaaaaa", 2, 12, "aaaaaaxaaaaaaa", 2, 12, -1)]     // Equal long alignment 0, different compare at n=5
        [InlineData("aaaaaaaaaaaaaa", 0, 13, "+aaaaaaaaaaaaa", 1, 13, 0)]      // Different int alignment, equal compare
        [InlineData("aaaaaaaaaaaaaa", 0, 13, "aaaaaaaaaaaaax", 1, 13, -1)]     // Different int alignment
        [InlineData("aaaaaaaaaaaaaa", 1, 13, "aaaxaaaaaaaaaa", 3, 11, -1)]     // Different long alignment, abs of 4, one of them is 2, different at n=1
        [InlineData("-aaaaaaaaaaaaa", 1, 10, "++++aaaaaaaaaa", 4, 10, 0)]      // Different long alignment, equal compare
        [InlineData("aaaaaaaaaaaaaa", 1, 10, "aaaaaaaaaaaaax", 4, 10, -1)]     // Different long alignment
        public static void TestCompare_Ordinal_Indexed(string str1, int offset1, int length1, string str2, int offset2, int length2, int expectedResult)
        {
            CompareInfo ci = CultureInfo.InvariantCulture.CompareInfo;
            Assert.Equal(expectedResult, NormalizeCompare(ci.Compare(str1, offset1, length1, str2, offset2, length2, CompareOptions.Ordinal)));

            if ((str1 == null || offset1 + length1 == str1.Length) &&
                (str2 == null || offset2 + length2 == str2.Length))
            {
                Assert.Equal(expectedResult, NormalizeCompare(ci.Compare(str1, offset1, str2, offset2, CompareOptions.Ordinal)));
            }
        }

        [Fact]
        public void Compare_Invalid()
        {
            // Compare options are invalid
            Assert.Throws<ArgumentException>(() => CultureInfo.InvariantCulture.CompareInfo.Compare("Test's", "Tests", (CompareOptions)(-1)));
        }

        private static char UnassignedUnicodeCharacter()
        {
            for (char ch = '\uFFFF'; ch > '\u0000'; ch++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.OtherNotAssigned)
                {
                    return ch;
                }
            }
            return char.MinValue; // There are no unassigned unicode characters from \u0000 - \uFFFF
        }

        private static int PredictCompareOrdinalResult(string string1, string string2)
        {
            if (string1 == null)
            {
                if (string2 == null) return 0;
                else return -1;
            }
            if (string2 == null) return 1;

            for (int i = 0; i < string1.Length; i++)
            {
                if (i >= string2.Length) return 1;
                if (string1[i] > string2[i]) return 1;
                if (string1[i] < string2[i]) return -1;
            }

            if (string2.Length > string1.Length) return -1;

            return 0;
        }

        private static int NormalizeCompare(int result)
        {
            return Math.Sign(result);
        }
    }
}
