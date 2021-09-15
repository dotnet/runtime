// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public partial class CompareInfoTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("en")]
        [InlineData("zh-Hans")]
        [InlineData("zh-Hant")]
        public void GetCompareInfo(string name)
        {
            CompareInfo compare = CompareInfo.GetCompareInfo(name);
            Assert.Equal(name, compare.Name);
        }

        [Fact]
        public void GetCompareInfo_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => CompareInfo.GetCompareInfo(null));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { CultureInfo.InvariantCulture.CompareInfo, CultureInfo.InvariantCulture.CompareInfo, true };
            yield return new object[] { CultureInfo.InvariantCulture.CompareInfo, CompareInfo.GetCompareInfo(""), true };
            yield return new object[] { new CultureInfo("en-US").CompareInfo, CompareInfo.GetCompareInfo("en-US"), true };
            yield return new object[] { new CultureInfo("en-US").CompareInfo, CompareInfo.GetCompareInfo("fr-FR"), false };
            yield return new object[] { new CultureInfo("en-US").CompareInfo, new object(), false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(CompareInfo compare1, object value, bool expected)
        {
            Assert.Equal(expected, compare1.Equals(value));
            if (value is CompareInfo)
            {
                Assert.Equal(expected, compare1.GetHashCode().Equals(value.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> GetHashCodeTestData => new[]
        {
            new object[] { "abc", CompareOptions.OrdinalIgnoreCase, "ABC", CompareOptions.OrdinalIgnoreCase, true },
            new object[] { "abc", CompareOptions.Ordinal, "ABC", CompareOptions.Ordinal, false },
            new object[] { "abc", CompareOptions.Ordinal, "abc", CompareOptions.Ordinal, true },
            new object[] { "abc", CompareOptions.None, "abc", CompareOptions.None, true },
            new object[] { "", CompareOptions.None, "\u200c", CompareOptions.None, true }, // see comment at bottom of SortKey_TestData
        };

        [Theory]
        [MemberData(nameof(GetHashCodeTestData))]
        public void GetHashCodeTest(string source1, CompareOptions options1, string source2, CompareOptions options2, bool expected)
        {
            CompareInfo invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
            Assert.Equal(expected, invariantCompare.GetHashCode(source1, options1).Equals(invariantCompare.GetHashCode(source2, options2)));
        }

        [Fact]
        public void GetHashCode_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode(null, CompareOptions.None));

            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test", CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreCase));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test", CompareOptions.Ordinal | CompareOptions.IgnoreSymbols));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test", (CompareOptions)(-1)));
        }

        [Theory]
        [InlineData("", "CompareInfo - ")]
        [InlineData("en-US", "CompareInfo - en-US")]
        [InlineData("EN-US", "CompareInfo - en-US")]
        public void ToStringTest(string name, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).CompareInfo.ToString());
        }

        public static IEnumerable<object[]> CompareInfo_TestData()
        {
            yield return new object[] { "en-US"  , 0x0409 };
            yield return new object[] { "ar-SA"  , 0x0401 };
            yield return new object[] { "ja-JP"  , 0x0411 };
            yield return new object[] { "zh-CN"  , 0x0804 };
            yield return new object[] { "en-GB"  , 0x0809 };
            yield return new object[] { "tr-TR"  , 0x041f };
        }

        // On NLS, hiragana characters sort after katakana.
        // On ICU, it is the opposite
        private static int s_expectedHiraganaToKatakanaCompare = PlatformDetection.IsNlsGlobalization ? 1 : -1;

        // On NLS, all halfwidth characters sort before fullwidth characters.
        // On ICU, half and fullwidth characters that aren't in the "Halfwidth and fullwidth forms" block U+FF00-U+FFEF
        // sort before the corresponding characters that are in the block U+FF00-U+FFEF
        private static int s_expectedHalfToFullFormsComparison = PlatformDetection.IsNlsGlobalization ? -1 : 1;

        private static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        private static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        private static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;

        public static IEnumerable<object[]> SortKey_Kana_TestData()
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
        }
        public static IEnumerable<object[]> SortKey_TestData()
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
            yield return new object[] { s_invariantCompare, "\"", "\uFF02", ignoreKanaIgnoreWidthIgnoreCase, 0 };

            if (!PlatformDetection.IsWindows7)
            {
                // For the below string, LCMapStringEx and CompareStringEx on Windows 7 return inconsistent results.
                // We'll only run this test case on Win8+ or on non-Windows machines.
                yield return new object[] { s_invariantCompare, "'", "\uFF07", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            }

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

            // Turkish
            yield return new object[] { s_turkishCompare, "i", "I", CompareOptions.None, 1 };
            // Android has its own ICU, which doesn't work well with tr
            if (!PlatformDetection.IsAndroid)
            {
                yield return new object[] { s_turkishCompare, "i", "I", CompareOptions.IgnoreCase, 1 };
                yield return new object[] { s_turkishCompare, "i", "\u0130", CompareOptions.IgnoreCase, 0 };
            }
            yield return new object[] { s_invariantCompare, "i", "\u0130", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "i", "I", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "i", "I", CompareOptions.IgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "i", "\u0130", CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "i", "\u0130", CompareOptions.IgnoreCase, -1 };

            yield return new object[] { s_invariantCompare, "\u00C0", "A\u0300", CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "\u00C0", "a\u0300", CompareOptions.None, 1 };
            yield return new object[] { s_invariantCompare, "\u00C0", "a\u0300", CompareOptions.IgnoreCase, 0 };
            yield return new object[] { s_invariantCompare, "FooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace, 0 };

            yield return new object[] { s_invariantCompare, "Test's", "Tests", CompareOptions.IgnoreSymbols, 0 };
            yield return new object[] { s_invariantCompare, "Test's", "Tests", CompareOptions.StringSort, -1 };

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

            // Zero-weight code points
            // In both NLS (Windows) and ICU the code point U+200C ZERO WIDTH NON-JOINER has a zero weight,
            // so it's compared as equal to the empty string. This means that we can't special-case GetHashCode("")
            // and return a fixed value; we actually need to call the underlying OS or ICU API to calculate the sort key.
            yield return new object[] { s_invariantCompare, "", "\u200c", CompareOptions.None, 0 };
        }

        public static IEnumerable<object[]> IndexOf_TestData()
        {
            yield return new object[] { s_invariantCompare, "foo", "", 0, 0, 1 };
            yield return new object[] { s_invariantCompare, "", "", 0, 0, 0 };
            yield return new object[] { s_invariantCompare, "Hello", "l", 0,  2, -1 };
            yield return new object[] { s_invariantCompare, "Hello", "l", 3,  3, 3 };
            yield return new object[] { s_invariantCompare, "Hello", "l", 2,  2, 2 };
            yield return new object[] { s_invariantCompare, "Hello", "L", 0, -1, -1 };
            yield return new object[] { s_invariantCompare, "Hello", "h", 0, -1, -1 };
        }

        public static IEnumerable<object[]> IsSortable_TestData()
        {
            yield return new object[] { "", false };
            yield return new object[] { "abcdefg", true };
            yield return new object[] { "\uD800\uDC00", true };

            // VS test runner for xunit doesn't handle ill-formed UTF-16 strings properly.
            // We'll send this one through as an array to avoid U+FFFD substitution.

            yield return new object[] { new char[] { '\uD800', '\uD800' }, false };
        }

        [Theory]
        [MemberData(nameof(CompareInfo_TestData))]
        public static void LcidTest(string cultureName, int lcid)
        {
            var ci = CompareInfo.GetCompareInfo(lcid);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);

            Assembly assembly = typeof(string).Assembly;

            ci = CompareInfo.GetCompareInfo(lcid, assembly);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);

            ci = CompareInfo.GetCompareInfo(cultureName, assembly);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);
        }

        [ConditionalTheory(typeof(CompareInfoCompareTests), nameof(CompareInfoCompareTests.IsNotWindowsKanaRegressedVersion))]
        [MemberData(nameof(SortKey_Kana_TestData))]
        public void SortKeyKanaTest(CompareInfo compareInfo, string string1, string string2, CompareOptions options, int expected)
        {
            SortKeyTest(compareInfo, string1, string2, options, expected);
        }


        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int CompareStringEx(string lpLocaleName, uint dwCmpFlags, string lpString1, int cchCount1, string lpString2, int cchCount2, IntPtr lpVersionInformation, IntPtr lpReserved, int lParam);
        private const int NORM_LINGUISTIC_CASING = 0x08000000;       // use linguistic rules for casing

        private static bool WindowsVersionHasTheCompareStringRegression =>
                    PlatformDetection.IsNlsGlobalization && CompareStringEx("", NORM_LINGUISTIC_CASING, "", 0, "\u200C", 1, IntPtr.Zero, IntPtr.Zero, 0) != 2;

        [Theory]
        [MemberData(nameof(SortKey_TestData))]
        public void SortKeyTest(CompareInfo compareInfo, string string1, string string2, CompareOptions options, int expectedSign)
        {
            SortKey sk1 = compareInfo.GetSortKey(string1, options);
            SortKey sk2 = compareInfo.GetSortKey(string2, options);

            Assert.Equal(expectedSign, Math.Sign(SortKey.Compare(sk1, sk2)));
            Assert.Equal(expectedSign == 0, sk1.Equals(sk2));

            if (!WindowsVersionHasTheCompareStringRegression)
            {
                Assert.Equal(Math.Sign(compareInfo.Compare(string1, string2, options)), Math.Sign(SortKey.Compare(sk1, sk2)));
            }

            Assert.Equal(compareInfo.GetHashCode(string1, options), sk1.GetHashCode());
            Assert.Equal(compareInfo.GetHashCode(string2, options), sk2.GetHashCode());

            Assert.Equal(string1, sk1.OriginalString);
            Assert.Equal(string2, sk2.OriginalString);

            // Now try the span-based versions - use BoundedMemory to detect buffer overruns

            RunSpanSortKeyTest(compareInfo, string1, options, sk1.KeyData);
            RunSpanSortKeyTest(compareInfo, string2, options, sk2.KeyData);

            unsafe static void RunSpanSortKeyTest(CompareInfo compareInfo, ReadOnlySpan<char> source, CompareOptions options, byte[] expectedSortKey)
            {
                using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData(source);
                sourceBoundedMemory.MakeReadonly();

                Assert.Equal(expectedSortKey.Length, compareInfo.GetSortKeyLength(sourceBoundedMemory.Span, options));

                using BoundedMemory<byte> sortKeyBoundedMemory = BoundedMemory.Allocate<byte>(expectedSortKey.Length);

                // First try with a destination which is too small - should result in an error

                Assert.Throws<ArgumentException>("destination", () => compareInfo.GetSortKey(sourceBoundedMemory.Span, sortKeyBoundedMemory.Span.Slice(1), options));

                // Next, try with a destination which is perfectly sized - should succeed

                Span<byte> sortKeyBoundedSpan = sortKeyBoundedMemory.Span;
                sortKeyBoundedSpan.Clear();

                Assert.Equal(expectedSortKey.Length, compareInfo.GetSortKey(sourceBoundedMemory.Span, sortKeyBoundedSpan, options));
                Assert.Equal(expectedSortKey, sortKeyBoundedSpan[0..expectedSortKey.Length].ToArray());
            }
        }

        [Fact]
        public void SortKeyMiscTest()
        {
            CompareInfo ci = new CultureInfo("en-US").CompareInfo;
            string s1 = "abc";
            string s2 = "ABC";

            SortKey sk1 = ci.GetSortKey(s1);
            SortKey sk2 = ci.GetSortKey(s1);

            SortKey sk3 = ci.GetSortKey(s2);
            SortKey sk4 = ci.GetSortKey(s2, CompareOptions.IgnoreCase);
            SortKey sk5 = ci.GetSortKey(s1, CompareOptions.IgnoreCase);

            Assert.Equal(sk2, sk1);
            Assert.Equal(sk2.GetHashCode(), sk1.GetHashCode());
            Assert.Equal(sk2.KeyData, sk1.KeyData);

            Assert.NotEqual(sk3, sk1);
            Assert.NotEqual(sk3.GetHashCode(), sk1.GetHashCode());
            Assert.NotEqual(sk3.KeyData, sk1.KeyData);

            Assert.NotEqual(sk4, sk3);
            Assert.NotEqual(sk4.GetHashCode(), sk3.GetHashCode());
            Assert.NotEqual(sk4.KeyData, sk3.KeyData);

            Assert.Equal(sk4, sk5);
            Assert.Equal(sk4.GetHashCode(), sk5.GetHashCode());
            Assert.Equal(sk4.KeyData, sk5.KeyData);

            Assert.False(sk1.Equals(null));
            Assert.True(sk1.Equals(sk1));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ci.GetSortKey(null));
            AssertExtensions.Throws<ArgumentException>("options", () => ci.GetSortKey(s1, CompareOptions.Ordinal));
        }

        [Theory]
        [MemberData(nameof(IndexOf_TestData))]
        public void IndexOfTest(CompareInfo compareInfo, string source, string value, int startIndex, int indexOfExpected, int lastIndexOfExpected)
        {
            Assert.Equal(indexOfExpected, compareInfo.IndexOf(source, value, startIndex));
            if (value.Length == 1)
            {
                Assert.Equal(indexOfExpected, compareInfo.IndexOf(source, value[0], startIndex));
            }

            Assert.Equal(lastIndexOfExpected, compareInfo.LastIndexOf(source, value, startIndex));
            if (value.Length == 1)
            {
                Assert.Equal(lastIndexOfExpected, compareInfo.LastIndexOf(source, value[0], startIndex));
            }
        }

        [Theory]
        [MemberData(nameof(IsSortable_TestData))]
        public void IsSortableTest(object sourceObj, bool expected)
        {
            string source = sourceObj as string ?? new string((char[])sourceObj);
            Assert.Equal(expected, CompareInfo.IsSortable(source));

            // Now test the span version - use BoundedMemory to detect buffer overruns

            using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(source);
            sourceBoundedMemory.MakeReadonly();
            Assert.Equal(expected, CompareInfo.IsSortable(sourceBoundedMemory.Span));

            // If the string as a whole is sortable, then all chars which aren't standalone
            // surrogate halves must also be sortable.

            foreach (char c in source)
                Assert.Equal(expected && !char.IsSurrogate(c), CompareInfo.IsSortable(c));
        }

        [Fact]
        public void VersionTest()
        {
            SortVersion sv1 = CultureInfo.GetCultureInfo("en-US").CompareInfo.Version;
            SortVersion sv2 = CultureInfo.GetCultureInfo("ja-JP").CompareInfo.Version;
            SortVersion sv3 = CultureInfo.GetCultureInfo("en").CompareInfo.Version;

            Assert.Equal(sv1.FullVersion, sv3.FullVersion);
            Assert.NotEqual(sv1.SortId, sv2.SortId);
        }

        [Theory]
        [MemberData(nameof(GetHashCodeTestData))]
        public void GetHashCode_Span(string source1, CompareOptions options1, string source2, CompareOptions options2, bool expectSameHashCode)
        {
            CompareInfo invariantCompare = CultureInfo.InvariantCulture.CompareInfo;

            int hashOfSource1AsString = invariantCompare.GetHashCode(source1, options1);
            int hashOfSource1AsSpan = invariantCompare.GetHashCode(source1.AsSpan(), options1);
            Assert.Equal(hashOfSource1AsString, hashOfSource1AsSpan);

            int hashOfSource2AsString = invariantCompare.GetHashCode(source2, options2);
            int hashOfSource2AsSpan = invariantCompare.GetHashCode(source2.AsSpan(), options2);
            Assert.Equal(hashOfSource2AsString, hashOfSource2AsSpan);

            Assert.Equal(expectSameHashCode, hashOfSource1AsSpan == hashOfSource2AsSpan);
        }

        [Fact]
        public void GetHashCode_NullAndEmptySpan()
        {
            // Ensure that null spans and non-null empty spans produce the same hash code.

            int hashCodeOfNullSpan = CultureInfo.InvariantCulture.CompareInfo.GetHashCode(ReadOnlySpan<char>.Empty, CompareOptions.None);
            int hashCodeOfNotNullEmptySpan = CultureInfo.InvariantCulture.CompareInfo.GetHashCode("".AsSpan(), CompareOptions.None);
            Assert.Equal(hashCodeOfNullSpan, hashCodeOfNotNullEmptySpan);
        }

        [Fact]
        public void GetHashCode_Span_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test".AsSpan(), CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreCase));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test".AsSpan(), CompareOptions.Ordinal | CompareOptions.IgnoreSymbols));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test".AsSpan(), (CompareOptions)(-1)));
        }
    }
}
