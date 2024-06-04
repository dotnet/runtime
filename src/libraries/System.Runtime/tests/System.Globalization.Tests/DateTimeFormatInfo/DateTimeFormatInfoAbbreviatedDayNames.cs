// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAbbreviatedDayNames
    {
        [Fact]
        public void AbbreviatedDayNames_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }, DateTimeFormatInfo.InvariantInfo.AbbreviatedDayNames);
        }

        [Fact]
        public void AbbreviatedDayNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.AbbreviatedDayNames, format.AbbreviatedDayNames);
            Assert.NotSame(format.AbbreviatedDayNames, format.AbbreviatedDayNames);
        }

        public static IEnumerable<object[]> AbbreviatedDayNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "" } };
        }

        public static IEnumerable<object[]> AbbreviatedDayNames_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { "ar-SA", new string[] { "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" } };
            yield return new object[] { "am-ET", new string[] { "እሑድ", "ሰኞ", "ማክሰ", "ረቡዕ", "ሐሙስ", "ዓርብ", "ቅዳሜ" } };
            yield return new object[] { "bg-BG", new string[] { "нд", "пн", "вт", "ср", "чт", "пт", "сб" } };
            yield return new object[] { "bn-BD", new string[] { "রবি", "সোম", "মঙ্গল", "বুধ", "বৃহস্পতি", "শুক্র", "শনি" } };
            yield return new object[] { "ca-AD", new string[] { "dg.", "dl.", "dt.", "dc.", "dj.", "dv.", "ds." } };
            yield return new object[] { "cs-CZ", new string[] { "ne", "po", "út", "st", "čt", "pá", "so" } };
            yield return new object[] { "da-DK", new string[] { "søn.", "man.", "tirs.", "ons.", "tors.", "fre.", "lør." } };
            yield return new object[] { "de-DE", new string[] { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" } };
            yield return new object[] { "el-GR", new string[] { "Κυρ", "Δευ", "Τρί", "Τετ", "Πέμ", "Παρ", "Σάβ" } };
            yield return new object[] { "en-CA", new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" } }; // should be with dots
            yield return new object[] { "en-US", new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" } };
            yield return new object[] { "es-419", new string[] { "dom", "lun", "mar", "mié", "jue", "vie", "sáb" } }; // should be with dots like all "es-*"
            yield return new object[] { "et-EE", new string[] { "P", "E", "T", "K", "N", "R", "L" } };
            yield return new object[] { "fa-IR", new string[] { "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه" } };
            yield return new object[] { "fi-FI", new string[] { "su", "ma", "ti", "ke", "to", "pe", "la" } };
            yield return new object[] { "fil-PH", new string[] { "Lin", "Lun", "Mar", "Miy", "Huw", "Biy", "Sab" } };
            yield return new object[] { "fr-BE", new string[] { "dim.", "lun.", "mar.", "mer.", "jeu.", "ven.", "sam." } };
            yield return new object[] { "gu-IN", new string[] { "રવિ", "સોમ", "મંગળ", "બુધ", "ગુરુ", "શુક્ર", "શનિ" } };
            yield return new object[] { "he-IL", new string[] { "יום א׳", "יום ב׳", "יום ג׳", "יום ד׳", "יום ה׳", "יום ו׳", "שבת" } };
            yield return new object[] { "hr-BA", new string[] { "ned", "pon", "uto", "sri", "čet", "pet", "sub" } };
            yield return new object[] { "hu-HU", new string[] { "V", "H", "K", "Sze", "Cs", "P", "Szo" } };
            yield return new object[] { "id-ID", new string[] { "Min", "Sen", "Sel", "Rab", "Kam", "Jum", "Sab" } };
            yield return new object[] { "it-CH", new string[] { "dom", "lun", "mar", "mer", "gio", "ven", "sab" } };
            yield return new object[] { "it-IT", new string[] { "dom", "lun", "mar", "mer", "gio", "ven", "sab" } };
            yield return new object[] { "ja-JP", new string[] { "日", "月", "火", "水", "木", "金", "土" } };
            yield return new object[] { "kn-IN", new string[] { "ಭಾನು", "ಸೋಮ", "ಮಂಗಳ", "ಬುಧ", "ಗುರು", "ಶುಕ್ರ", "ಶನಿ" } };
            yield return new object[] { "ko-KR", new string[] { "일", "월", "화", "수", "목", "금", "토" } };
            yield return new object[] { "lt-LT", new string[] { "sk", "pr", "an", "tr", "kt", "pn", "št" } };
            yield return new object[] { "lv-LV", new string[] { "Svētd.", "Pirmd.", "Otrd.", "Trešd.", "Ceturtd.", "Piektd.", "Sestd." } };
            yield return new object[] { "ml-IN", new string[] { "ഞായർ", "തിങ്കൾ", "ചൊവ്വ", "ബുധൻ", "വ്യാഴം", "വെള്ളി", "ശനി" } };
            yield return new object[] { "mr-IN", new string[] { "रवि", "सोम", "मंगळ", "बुध", "गुरु", "शुक्र", "शनि" } };
            yield return new object[] { "ms-BN", new string[] { "Ahd", "Isn", "Sel", "Rab", "Kha", "Jum", "Sab" } };
            yield return new object[] { "nb-NO", new string[] { "søn.", "man.", "tir.", "ons.", "tor.", "fre.", "lør." } };
            yield return new object[] { "nl-AW", new string[] { "zo", "ma", "di", "wo", "do", "vr", "za" } };
            yield return new object[] { "pl-PL", new string[] { "niedz.", "pon.", "wt.", "śr.", "czw.", "pt.", "sob." } };
            yield return new object[] { "pt-BR", new string[] { "dom.", "seg.", "ter.", "qua.", "qui.", "sex.", "sáb." } };
            yield return new object[] { "pt-PT", new string[] { "domingo", "segunda", "terça", "quarta", "quinta", "sexta", "sábado" } };
            yield return new object[] { "ro-RO", new string[] { "dum.", "lun.", "mar.", "mie.", "joi", "vin.", "sâm." } };
            yield return new object[] { "ru-RU", new string[] { "вс", "пн", "вт", "ср", "чт", "пт", "сб" } };
            yield return new object[] { "sk-SK", new string[] { "ne", "po", "ut", "st", "št", "pi", "so" } };
            yield return new object[] { "sl-SI", new string[] { "ned.", "pon.", "tor.", "sre.", "čet.", "pet.", "sob." } };
            yield return new object[] { "sr-Cyrl-RS", new string[] { "нед", "пон", "уто", "сре", "чет", "пет", "суб" } };
            yield return new object[] { "sr-Latn-RS", new string[] { "ned", "pon", "uto", "sre", "čet", "pet", "sub" } };
            yield return new object[] { "sv-AX", new string[] { "sön", "mån", "tis", "ons", "tors", "fre", "lör" } };
            yield return new object[] { "sv-SE", new string[] { "sön", "mån", "tis", "ons", "tors", "fre", "lör" } };
            yield return new object[] { "sw-CD", new string[] { "Jumapili", "Jumatatu", "Jumanne", "Jumatano", "Alhamisi", "Ijumaa", "Jumamosi" } };
            yield return new object[] { "ta-IN", new string[] { "ஞாயி.", "திங்.", "செவ்.", "புத.", "வியா.", "வெள்.", "சனி" } };
            yield return new object[] { "te-IN", new string[] { "ఆది", "సోమ", "మంగళ", "బుధ", "గురు", "శుక్ర", "శని" } };
            yield return new object[] { "th-TH", new string[] { "อา.", "จ.", "อ.", "พ.", "พฤ.", "ศ.", "ส." } };
            yield return new object[] { "tr-CY", new string[] { "Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt" } };
            yield return new object[] { "uk-UA", new string[] { "нд", "пн", "вт", "ср", "чт", "пт", "сб" } };
            yield return new object[] { "vi-VN", new string[] { "CN", "Th 2", "Th 3", "Th 4", "Th 5", "Th 6", "Th 7" } };
            yield return new object[] { "zh-CN", new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(AbbreviatedDayNames_Get_TestData_HybridGlobalization))]
        public void AbbreviatedDayNames_Get_ReturnsExpected_HybridGlobalization(string cultureName, string[] expected)
        {
            var format = new CultureInfo(cultureName).DateTimeFormat;
            int length = format.AbbreviatedDayNames.Length;
            Assert.True(length == expected.Length, $"Length comparison failed for culture: {cultureName}. Expected: {expected.Length}, Actual: {length}");
            for (int i = 0; i<length; i++)
                Assert.True(expected[i] == format.AbbreviatedDayNames[i], $"Failed for culture: {cultureName} on index: {i}. Expected: {expected[i]}, Actual: {format.AbbreviatedDayNames[i]}");
        }

        [Theory]
        [MemberData(nameof(AbbreviatedDayNames_Set_TestData))]
        public void AbbreviatedDayNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.AbbreviatedDayNames = value;
            Assert.Equal(value, format.AbbreviatedDayNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.AbbreviatedDayNames);
            Assert.Equal(value, format.AbbreviatedDayNames);
        }

        [Fact]
        public void AbbreviatedDayNames_SetNulValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedDayNames = null);
        }

        [Fact]
        public void AbbreviatedDayNames_SetNulValueInValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedDayNames = new string[] { "1", "2", "3", null, "5", "6", "7" });
        }

        public static IEnumerable<object[]> AbbreviatedDayNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Sun" } };
            yield return new object[] { new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(AbbreviatedDayNames_SetInvalidLength_TestData))]
        public void AbbreviatedDayNames_SetInvalidLength_ThrowsArgumentException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", (() => format.AbbreviatedDayNames = value));
        }

        [Fact]
        public void AbbreviatedDayNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.AbbreviatedDayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" });
        }
    }
}
