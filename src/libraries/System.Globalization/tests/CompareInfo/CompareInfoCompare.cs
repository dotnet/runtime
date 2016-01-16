// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoCompare
    {
        private static bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // on windows hiragana characters sort after katakana. on ICU, it is the opposite
        private static int s_expectedHiraganaToKatakanaCompare = s_isWindows ? 1 : -1;

        // on windows, all halfwidth characters sort before fullwidth characters.
        // on ICU, half and fullwidth characters that aren't in the "Halfwidth and fullwidth forms" block U+FF00-U+FFEF
        // sort before the corresponding characters that are in the block U+FF00-U+FFEF
        private static int s_expectedHalfToFullFormsComparison = s_isWindows ? -1 : 1;

        private const string c_SoftHyphen = "\u00AD";

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test1() { TestOrd(CultureInfo.InvariantCulture, "\u3042", "\u30A1", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test2() { TestOrd(CultureInfo.InvariantCulture, "\u3042", "\u30A2", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test3() { TestOrd(CultureInfo.InvariantCulture, "\u3042", "\uFF71", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test4() { TestOrd(CultureInfo.InvariantCulture, "\u304D\u3083", "\u30AD\u30E3", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test5() { TestOrd(CultureInfo.InvariantCulture, "\u304D\u3083", "\u30AD\u3083", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test6() { TestOrd(CultureInfo.InvariantCulture, "\u304D \u3083", "\u30AD\u3083", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test7() { TestOrd(CultureInfo.InvariantCulture, "\u3044", "I", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test8() { TestOrd(CultureInfo.InvariantCulture, "a", "A", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test9() { TestOrd(CultureInfo.InvariantCulture, "a", "\uFF41", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test10() { TestOrd(CultureInfo.InvariantCulture, "ABCDE", "\uFF21\uFF22\uFF23\uFF24\uFF25", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test11() { TestOrd(CultureInfo.InvariantCulture, "ABCDE", "\uFF21\uFF22\uFF23D\uFF25", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test12() { TestOrd(CultureInfo.InvariantCulture, "ABCDE", "a\uFF22\uFF23D\uFF25", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test13() { TestOrd(CultureInfo.InvariantCulture, "ABCDE", "\uFF41\uFF42\uFF23D\uFF25", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test14() { TestOrd(CultureInfo.InvariantCulture, "\u6FA4", "\u6CA2", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test15() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u30D6\u30D9\u30DC", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test16() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\u30DC", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test17() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test18() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test19() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test20() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test21() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3079\uFF8E\uFF9E", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test22() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test23() { TestOrd(CultureInfo.InvariantCulture, "\u3071\u3074\u30D7\u307A", "\uFF8B\uFF9F\uFF8C\uFF9F", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test24() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3070\uFF8E\uFF9E\u30D6", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test25() { TestOrd(CultureInfo.InvariantCulture, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C\u3079\u307C", "\u3079\uFF8E\uFF9E", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test26() { TestOrd(CultureInfo.InvariantCulture, "\u3070\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test27() { TestOrd(CultureInfo.InvariantCulture, "ABDDE", "D", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test28() { TestOrd(CultureInfo.InvariantCulture, "ABCDE", "\uFF43D\uFF25", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test29() { TestOrd(CultureInfo.InvariantCulture, "ABCDE", "\uFF43D", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test30() { TestOrd(CultureInfo.InvariantCulture, "ABCDE", "c", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test31() { TestOrd(CultureInfo.InvariantCulture, "\u3060", "\u305F", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test32() { TestOrd(CultureInfo.InvariantCulture, "\u3060", "\uFF80\uFF9E", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test33() { TestOrd(CultureInfo.InvariantCulture, "\u3060", "\u30C0", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test34() { TestOrd(CultureInfo.InvariantCulture, "\u30C7\u30BF\u30D9\u30B9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E\uFF7D", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test35() { TestOrd(CultureInfo.InvariantCulture, "\u30C7", "\uFF83\uFF9E", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test36() { TestOrd(CultureInfo.InvariantCulture, "\u30C7\u30BF", "\uFF83\uFF9E\uFF80", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test37() { TestOrd(CultureInfo.InvariantCulture, "\u30C7\u30BF\u30D9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test38() { TestOrd(CultureInfo.InvariantCulture, "\u30BF", "\uFF80", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test39() { TestOrd(CultureInfo.InvariantCulture, "\uFF83\uFF9E\uFF70\uFF80\uFF8D\uFF9E\uFF70\uFF7D", "\u3067\u30FC\u305F\u3079\u30FC\u3059", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test40() { TestOrd(CultureInfo.InvariantCulture, "\u68EE\u9D0E\u5916", "\u68EE\u9DD7\u5916", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test41() { TestOrd(CultureInfo.InvariantCulture, "\u68EE\u9DD7\u5916", "\u68EE\u9DD7\u5916", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test42() { TestOrd(CultureInfo.InvariantCulture, "\u2019\u2019\u2019\u2019", "''''", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test43() { TestOrd(CultureInfo.InvariantCulture, "\u2019", "'", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test44() { TestOrd(CultureInfo.InvariantCulture, "", "'", -1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test45() { TestOrd(CultureInfo.InvariantCulture, "\u4E00", "\uFF11", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test46() { TestOrd(CultureInfo.InvariantCulture, "\u2160", "\uFF11", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test47() { TestOrd(CultureInfo.InvariantCulture, "0", "\uFF10", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test48() { TestOrd(CultureInfo.InvariantCulture, "10", "1\uFF10", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test49() { TestOrd(CultureInfo.InvariantCulture, "1\uFF10", "1\uFF10", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test50() { TestOrd(CultureInfo.InvariantCulture, "9999\uFF1910", "1\uFF10", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test51() { TestOrd(CultureInfo.InvariantCulture, "9999\uFF191010", "1\uFF10", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test52() { TestOrd(CultureInfo.InvariantCulture, "'\u3000'", "' '", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test53() { TestOrd(CultureInfo.InvariantCulture, "'\u3000'", "''", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test54() { TestOrd(CultureInfo.InvariantCulture, "\uFF1B", ";", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test55() { TestOrd(CultureInfo.InvariantCulture, "\uFF08", "(", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test56() { TestOrd(CultureInfo.InvariantCulture, "\u30FC", "\uFF70", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test57() { TestOrd(CultureInfo.InvariantCulture, "\u30FC", "\uFF0D", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test58() { TestOrd(CultureInfo.InvariantCulture, "\u30FC", "\u30FC", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test59() { TestOrd(CultureInfo.InvariantCulture, "\u30FC", "\u2015", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test60() { TestOrd(CultureInfo.InvariantCulture, "\u30FC", "\u2010", 1, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test61() { TestOrd(CultureInfo.InvariantCulture, "/", "\uFF0F", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test62() { TestOrd(CultureInfo.InvariantCulture, "'", "\uFF07", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test63() { TestOrd(CultureInfo.InvariantCulture, "\"", "\uFF02", 0, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test64() { Test(CultureInfo.InvariantCulture, "\u3042", "\u30A1", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test65() { Test(CultureInfo.InvariantCulture, "\u3042", "\u30A2", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test66() { Test(CultureInfo.InvariantCulture, "\u3042", "\uFF71", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test67() { Test(CultureInfo.InvariantCulture, "\u304D\u3083", "\u30AD\u30E3", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test68() { Test(CultureInfo.InvariantCulture, "\u304D\u3083", "\u30AD\u3083", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test69() { Test(CultureInfo.InvariantCulture, "\u304D \u3083", "\u30AD\u3083", -1, CompareOptions.None); }

        [Fact]
        public void Test70() { Test(CultureInfo.InvariantCulture, "\u3044", "I", 1, CompareOptions.None); }

        [Fact]
        public void Test71() { Test(CultureInfo.InvariantCulture, "a", "A", -1, CompareOptions.None); }

        [Fact]
        public void Test72() { Test(CultureInfo.InvariantCulture, "a", "\uFF41", -1, CompareOptions.None); }

        [Fact]
        public void Test73() { Test(CultureInfo.InvariantCulture, "ABCDE", "\uFF21\uFF22\uFF23\uFF24\uFF25", -1, CompareOptions.None); }

        [Fact]
        public void Test74() { Test(CultureInfo.InvariantCulture, "ABCDE", "\uFF21\uFF22\uFF23D\uFF25", -1, CompareOptions.None); }

        [Fact]
        public void Test75() { Test(CultureInfo.InvariantCulture, new string('a', 5555), new string('a', 5554) + "b", -1, CompareOptions.None); }

        [Fact]
        public void Test76() { Test(CultureInfo.InvariantCulture, "ABCDE", "\uFF41\uFF42\uFF23D\uFF25", 1, CompareOptions.None); }

        [Fact]
        public void Test77() { Test(CultureInfo.InvariantCulture, "\u6FA4", "\u6CA2", 1, CompareOptions.None); }

        [Fact]
        public void Test78() { Test(CultureInfo.InvariantCulture, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u30D6\u30D9\u30DC", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test79() { Test(CultureInfo.InvariantCulture, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\u30DC", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test80() { Test(CultureInfo.InvariantCulture, "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test81() { Test(CultureInfo.InvariantCulture, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test82() { Test(CultureInfo.InvariantCulture, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", -1, CompareOptions.None); }

        [Fact]
        public void Test83() { Test(CultureInfo.InvariantCulture, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", -1, CompareOptions.None); }

        [Fact]
        public void Test84() { Test(CultureInfo.InvariantCulture, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3079\uFF8E\uFF9E", -1, CompareOptions.None); }

        [Fact]
        public void Test85() { Test(CultureInfo.InvariantCulture, "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", -1, CompareOptions.None); }

        [Fact]
        public void Test86() { Test(CultureInfo.InvariantCulture, "\u3071\u3074\u30D7\u307A", "\uFF8B\uFF9F\uFF8C\uFF9F", -1, CompareOptions.None); }

        [Fact]
        public void Test87() { Test(CultureInfo.InvariantCulture, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3070\uFF8E\uFF9E\u30D6", 1, CompareOptions.None); }

        [Fact]
        public void Test88() { Test(CultureInfo.InvariantCulture, "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C\u3079\u307C", "\u3079\uFF8E\uFF9E", -1, CompareOptions.None); }

        [Fact]
        public void Test89() { Test(CultureInfo.InvariantCulture, "\u3070\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", -1, CompareOptions.None); }

        [Fact]
        public void Test90() { Test(CultureInfo.InvariantCulture, "ABDDE", "D", -1, CompareOptions.None); }

        [Fact]
        public void Test91() { Test(CultureInfo.InvariantCulture, "ABCDE", "\uFF43D\uFF25", -1, CompareOptions.None); }

        [Fact]
        public void Test92() { Test(CultureInfo.InvariantCulture, "ABCDE", "\uFF43D", -1, CompareOptions.None); }

        [Fact]
        public void Test93() { Test(CultureInfo.InvariantCulture, "ABCDE", "c", -1, CompareOptions.None); }

        [Fact]
        public void Test94() { Test(CultureInfo.InvariantCulture, "\u3060", "\u305F", 1, CompareOptions.None); }

        [Fact]
        public void Test95() { Test(CultureInfo.InvariantCulture, "\u3060", "\uFF80\uFF9E", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        public void Test96() { Test(CultureInfo.InvariantCulture, "\u3060", "\u30C0", s_expectedHiraganaToKatakanaCompare, CompareOptions.None); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test97() { Test(CultureInfo.InvariantCulture, "\u30C7\u30BF\u30D9\u30B9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E\uFF7D", 1, CompareOptions.None); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test98() { Test(CultureInfo.InvariantCulture, "\u30C7", "\uFF83\uFF9E", 1, CompareOptions.None); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test99() { Test(CultureInfo.InvariantCulture, "\u30C7\u30BF", "\uFF83\uFF9E\uFF80", 1, CompareOptions.None); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test100() { Test(CultureInfo.InvariantCulture, "\u30C7\u30BF\u30D9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E", 1, CompareOptions.None); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test101() { Test(CultureInfo.InvariantCulture, "\u30BF", "\uFF80", 1, CompareOptions.None); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test102() { Test(CultureInfo.InvariantCulture, "\uFF83\uFF9E\uFF70\uFF80\uFF8D\uFF9E\uFF70\uFF7D", "\u3067\u30FC\u305F\u3079\u30FC\u3059", -1, CompareOptions.None); }

        [Fact]
        public void Test103() { Test(CultureInfo.InvariantCulture, "\u68EE\u9D0E\u5916", "\u68EE\u9DD7\u5916", -1, CompareOptions.None); }

        [Fact]
        public void Test104() { Test(CultureInfo.InvariantCulture, "\u68EE\u9DD7\u5916", "\u68EE\u9DD7\u5916", 0, CompareOptions.None); }

        [Fact]
        public void Test105() { Test(CultureInfo.InvariantCulture, "\u2019\u2019\u2019\u2019", "''''", 1, CompareOptions.None); }

        [Fact]
        public void Test106() { Test(CultureInfo.InvariantCulture, "\u2019", "'", 1, CompareOptions.None); }

        [Fact]
        public void Test107() { Test(CultureInfo.InvariantCulture, "", "'", -1, CompareOptions.None); }

        [Fact]
        public void Test108() { Test(CultureInfo.InvariantCulture, "\u4E00", "\uFF11", 1, CompareOptions.None); }

        [Fact]
        public void Test109() { Test(CultureInfo.InvariantCulture, "\u2160", "\uFF11", 1, CompareOptions.None); }

        [Fact]
        public void Test110() { Test(CultureInfo.InvariantCulture, "0", "\uFF10", -1, CompareOptions.None); }

        [Fact]
        public void Test111() { Test(CultureInfo.InvariantCulture, "10", "1\uFF10", -1, CompareOptions.None); }

        [Fact]
        public void Test112() { Test(CultureInfo.InvariantCulture, "1\uFF10", "1\uFF10", 0, CompareOptions.None); }

        [Fact]
        public void Test113() { Test(CultureInfo.InvariantCulture, "9999\uFF1910", "1\uFF10", 1, CompareOptions.None); }

        [Fact]
        public void Test114() { Test(CultureInfo.InvariantCulture, "9999\uFF191010", "1\uFF10", 1, CompareOptions.None); }

        [Fact]
        public void Test115() { Test(CultureInfo.InvariantCulture, "'\u3000'", "' '", 1, CompareOptions.None); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test116() { Test(CultureInfo.InvariantCulture, "'\u3000'", "''", 1, CompareOptions.None); }

        [Fact]
        public void Test117() { Test(CultureInfo.InvariantCulture, "\uFF1B", ";", 1, CompareOptions.None); }

        [Fact]
        public void Test118() { Test(CultureInfo.InvariantCulture, "\uFF08", "(", 1, CompareOptions.None); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test119() { Test(CultureInfo.InvariantCulture, "\u30FC", "\uFF70", 0, CompareOptions.None); }

        [Fact]
        public void Test120() { Test(CultureInfo.InvariantCulture, "\u30FC", "\uFF0D", 1, CompareOptions.None); }

        [Fact]
        public void Test121() { Test(CultureInfo.InvariantCulture, "\u30FC", "\u30FC", 0, CompareOptions.None); }

        [Fact]
        public void Test122() { Test(CultureInfo.InvariantCulture, "\u30FC", "\u2015", 1, CompareOptions.None); }

        [Fact]
        public void Test123() { Test(CultureInfo.InvariantCulture, "\u30FC", "\u2010", 1, CompareOptions.None); }

        [Fact]
        public void Test124() { Test(CultureInfo.InvariantCulture, "/", "\uFF0F", -1, CompareOptions.None); }

        [Fact]
        public void Test125() { Test(CultureInfo.InvariantCulture, "'", "\uFF07", -1, CompareOptions.None); }

        [Fact]
        public void Test126() { Test(CultureInfo.InvariantCulture, "\"", "\uFF02", -1, CompareOptions.None); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test127() { Test(new CultureInfo("hu-HU"), "dzsdzs", "ddzs", 0, CompareOptions.None); }

        [Fact]
        public void Test128() { TestOrd(new CultureInfo("hu-HU"), "dzsdzs", "ddzs", 1, CompareOptions.Ordinal); }

        [Fact]
        public void Test129() { Test(CultureInfo.InvariantCulture, "dzsdzs", "ddzs", 1, CompareOptions.None); }

        [Fact]
        public void Test130() { TestOrd(CultureInfo.InvariantCulture, "dzsdzs", "ddzs", 1, CompareOptions.Ordinal); }

        [Fact]
        public void Test131() { Test(new CultureInfo("tr-TR"), "i", "I", 1, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test132() { Test(CultureInfo.InvariantCulture, "i", "I", 0, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test133() { Test(new CultureInfo("tr-TR"), "i", "\u0130", 0, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test134() { Test(CultureInfo.InvariantCulture, "i", "\u0130", -1, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test135() { Test(new CultureInfo("tr-TR"), "i", "I", 1, CompareOptions.None); }

        [Fact]
        public void Test136() { Test(CultureInfo.InvariantCulture, "i", "I", -1, CompareOptions.None); }

        [Fact]
        public void Test137() { Test(new CultureInfo("tr-TR"), "i", "\u0130", -1, CompareOptions.None); }

        [Fact]
        public void Test138() { Test(CultureInfo.InvariantCulture, "i", "\u0130", -1, CompareOptions.None); }

        [Fact]
        public void Test139() { Test(CultureInfo.InvariantCulture, "\u00C0", "A\u0300", 0, CompareOptions.None); }

        [Fact]
        public void Test140() { TestOrd(CultureInfo.InvariantCulture, "\u00C0", "A\u0300", 1, CompareOptions.Ordinal); }

        [Fact]
        public void Test141() { Test(CultureInfo.InvariantCulture, "\u00C0", "a\u0300", 0, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test142() { TestOrd(CultureInfo.InvariantCulture, "\u00C0", "a\u0300", 1, CompareOptions.OrdinalIgnoreCase); }

        [Fact]
        public void Test143() { Test(CultureInfo.InvariantCulture, "\u00C0", "a\u0300", 1, CompareOptions.None); }

        [Fact]
        public void Test144() { TestOrd(CultureInfo.InvariantCulture, "\u00C0", "a\u0300", 1, CompareOptions.Ordinal); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test145()
        {
            char unassignedUnicode = GetNextUnassignedUnicode();
            Test(CultureInfo.InvariantCulture, "FooBar", "Foo" + unassignedUnicode + "Bar", 0, CompareOptions.None);
        }

        [Fact]
        public void Test146() { TestOrd(CultureInfo.InvariantCulture, "FooBar", "Foo\u0400Bar", -1, CompareOptions.Ordinal); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test147()
        {
            char unassignedUnicode = GetNextUnassignedUnicode();
            Test(CultureInfo.InvariantCulture, "FooBar", "Foo" + unassignedUnicode + "Bar", 0, CompareOptions.IgnoreNonSpace);
        }

        [Fact]
        public void Test148()
        {
            Test(CultureInfo.InvariantCulture, "FooBA\u0300R", "FooB\u00C0R", 0, CompareOptions.IgnoreNonSpace);
        }

        [Fact]
        public void Test149() { Test(CultureInfo.InvariantCulture, "Test's", "Tests", 0, CompareOptions.IgnoreSymbols); }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void Test150() { Test(CultureInfo.InvariantCulture, "Test's", "Tests", 1, CompareOptions.None); }

        [Fact]
        public void Test151() { Test(CultureInfo.InvariantCulture, "Test's", "Tests", -1, CompareOptions.StringSort); }

        [Fact]
        public void Test152() { Assert.Throws<ArgumentException>(() => { int i = CultureInfo.InvariantCulture.CompareInfo.Compare("Test's", "Tests", (CompareOptions)(-1)); }); }

        [Fact]
        public void Test153() { TestOrd(CultureInfo.InvariantCulture, null, "Tests", -1, CompareOptions.None); }

        [Fact]
        public void Test154() { TestOrd(CultureInfo.InvariantCulture, "Test's", null, 1, CompareOptions.None); }

        [Fact]
        public void Test155() { TestOrd(CultureInfo.InvariantCulture, null, null, 0, CompareOptions.None); }

        [Fact]
        public void Test156() { Test(CultureInfo.InvariantCulture, new string('a', 5555), new string('a', 5555), 0, CompareOptions.None); }

        [Fact]
        public void Test157() { Test(CultureInfo.InvariantCulture, "foobar", "FooB\u00C0R", 0, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase); }

        [Fact]
        public void Test158() { Test(CultureInfo.InvariantCulture, "foobar", "FooB\u00C0R", -1, CompareOptions.IgnoreNonSpace); }

        [Fact]
        public void Test159() { Test(CultureInfo.InvariantCulture, "\uFF9E", "\u3099", 0, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test160() { Test(CultureInfo.InvariantCulture, "\uFF9E", "\u3099", 0, CompareOptions.IgnoreNonSpace); }

        [Fact]
        public void Test161() { Test(CultureInfo.InvariantCulture, "\u20A9", "\uFFE6", 0, CompareOptions.IgnoreWidth); }

        [Fact]
        public void Test162() { Test(CultureInfo.InvariantCulture, "\u20A9", "\uFFE6", -1, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test163() { Test(CultureInfo.InvariantCulture, "\u20A9", "\uFFE6", -1, CompareOptions.None); }

        [Fact]
        public void Test164() { Test(CultureInfo.InvariantCulture, "\u0021", "\uFF01", 0, CompareOptions.IgnoreSymbols); }

        [Fact]
        public void Test165() { Test(CultureInfo.InvariantCulture, "\u00A2", "\uFFE0", 0, CompareOptions.IgnoreSymbols); }

        [Fact]
        public void Test166() { Test(CultureInfo.InvariantCulture, "$", "&", 0, CompareOptions.IgnoreSymbols); }

        [Fact]
        public void Test167() { Test(CultureInfo.InvariantCulture, "\uFF65", "\u30FB", 0, CompareOptions.IgnoreSymbols); }

        [Fact]
        public void Test168() { Test(CultureInfo.InvariantCulture, "\u0021", "\uFF01", 0, CompareOptions.IgnoreWidth); }

        [Fact]
        public void Test169() { Test(CultureInfo.InvariantCulture, "\u0021", "\uFF01", -1, CompareOptions.None); }

        [Fact]
        public void Test170() { Test(CultureInfo.InvariantCulture, "\uFF66", "\u30F2", 0, CompareOptions.IgnoreWidth); }

        [Fact]
        public void Test171() { Test(CultureInfo.InvariantCulture, "\uFF66", "\u30F2", s_expectedHalfToFullFormsComparison, CompareOptions.IgnoreSymbols); }

        [Fact]
        public void Test172() { Test(CultureInfo.InvariantCulture, "\uFF66", "\u30F2", s_expectedHalfToFullFormsComparison, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test173() { Test(CultureInfo.InvariantCulture, "\uFF66", "\u30F2", s_expectedHalfToFullFormsComparison, CompareOptions.IgnoreNonSpace); }

        [Fact]
        public void Test174() { Test(CultureInfo.InvariantCulture, "\uFF66", "\u30F2", s_expectedHalfToFullFormsComparison, CompareOptions.None); }

        [Fact]
        public void Test175() { Test(CultureInfo.InvariantCulture, "\u3060", "\u30C0", 0, CompareOptions.IgnoreKanaType); }

        [Fact]
        public void Test176() { Test(CultureInfo.InvariantCulture, "\u3060", "\u30C0", s_expectedHiraganaToKatakanaCompare, CompareOptions.IgnoreCase); }

        [Fact]
        public void Test177() { Test(CultureInfo.InvariantCulture, "c", "C", -1, CompareOptions.IgnoreKanaType); }

        [Theory]
        [InlineData(null, 0, 0, null, 0, 0, 0)]
        [InlineData("Hello", 0, 5, null, 0, 0, 1)]
        [InlineData(null, 0, 0, "Hello", 0, 5, -1)]
        [InlineData("Hello", 0, 0, "Hello", 0, 0, 0)]
        [InlineData("Hello", 0, 5, "Hello", 0, 5, 0)]
        [InlineData("Hello", 0, 3, "Hello", 0, 3, 0)]
        [InlineData("Hello", 2, 3, "Hello", 2, 3, 0)]
        [InlineData("Hello", 0, 5, "He" + c_SoftHyphen + "llo", 0, 5, -1)]
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
        public static void TestCompareOrdinalIndexed(string str1, int offset1, int length1, string str2, int offset2, int length2, int expectedResult)
        {
            CompareInfo ci = CultureInfo.InvariantCulture.CompareInfo;
            int i = NormalizeCompare(ci.Compare(str1, offset1, length1, str2, offset2, length2, CompareOptions.Ordinal));
            Assert.Equal(expectedResult, i);

            if ((str1 == null || offset1 + length1 == str1.Length) &&
                (str2 == null || offset2 + length2 == str2.Length))
            {
                i = NormalizeCompare(ci.Compare(str1, offset1, str2, offset2, CompareOptions.Ordinal));
                Assert.Equal(expectedResult, i);
            }
        }

        public void Test(CultureInfo culture, string str1, string str2, int expected, CompareOptions options)
        {
            CompareInfo ci = culture.CompareInfo;
            int i = ci.Compare(str1, str2, options);
            i = Math.Sign(i);
            Assert.Equal(expected, i);
            i = ci.Compare(str2, str1, options);
            i = Math.Sign(i);
            Assert.Equal((0 - expected), i);
        }

        public void TestOrd(CultureInfo culture, string str1, string str2, int expected, CompareOptions options)
        {
            CompareInfo ci = culture.CompareInfo;
            int i = ci.Compare(str1, str2, options);
            i = Math.Sign(i);
            Assert.Equal(expected, i);

            i = ci.Compare(str2, str1, options);
            i = Math.Sign(i);
            Assert.Equal((0 - expected), i);
        }

        private char GetNextUnassignedUnicode()
        {
            for (char ch = '\uFFFF'; ch > '\u0000'; ch++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.OtherNotAssigned)
                {
                    return ch;
                }
            }
            return Char.MinValue; // there are no unassigned unicode characters from \u0000 - \uFFFF
        }

        private static int NormalizeCompare(int i)
        {
            return
                i == 0 ? 0 :
                i > 0 ? 1 :
                -1;
        }
    }
}
