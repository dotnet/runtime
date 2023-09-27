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
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, new string[] { "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" } };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, new string[] { "እሑድ", "ሰኞ", "ማክሰ", "ረቡዕ", "ሐሙስ", "ዓርብ", "ቅዳሜ" } };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, new string[] { "нд", "пн", "вт", "ср", "чт", "пт", "сб" } };
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, new string[] { "রবি", "সোম", "মঙ্গল", "বুধ", "বৃহস্পতি", "শুক্র", "শনি" } };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, new string[] { "dg.", "dl.", "dt.", "dc.", "dj.", "dv.", "ds." } };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, new string[] { "ne", "po", "út", "st", "čt", "pá", "so" } };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, new string[] { "søn.", "man.", "tirs.", "ons.", "tors.", "fre.", "lør." } };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, new string[] { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" } };
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, new string[] { "Κυρ", "Δευ", "Τρί", "Τετ", "Πέμ", "Παρ", "Σάβ" } };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" } }; // should be with dots
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" } };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, new string[] { "dom", "lun", "mar", "mié", "jue", "vie", "sáb" } }; // should be with dots like all "es-*"
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, new string[] { "P", "E", "T", "K", "N", "R", "L" } };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, new string[] { "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه" } };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, new string[] { "su", "ma", "ti", "ke", "to", "pe", "la" } };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, new string[] { "Lin", "Lun", "Mar", "Miy", "Huw", "Biy", "Sab" } };
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, new string[] { "dim.", "lun.", "mar.", "mer.", "jeu.", "ven.", "sam." } };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, new string[] { "રવિ", "સોમ", "મંગળ", "બુધ", "ગુરુ", "શુક્ર", "શનિ" } };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, new string[] { "יום א׳", "יום ב׳", "יום ג׳", "יום ד׳", "יום ה׳", "יום ו׳", "שבת" } };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, new string[] { "ned", "pon", "uto", "sri", "čet", "pet", "sub" } };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, new string[] { "V", "H", "K", "Sze", "Cs", "P", "Szo" } };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, new string[] { "Min", "Sen", "Sel", "Rab", "Kam", "Jum", "Sab" } };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, new string[] { "dom", "lun", "mar", "mer", "gio", "ven", "sab" } };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, new string[] { "dom", "lun", "mar", "mer", "gio", "ven", "sab" } };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, new string[] { "日", "月", "火", "水", "木", "金", "土" } };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, new string[] { "ಭಾನು", "ಸೋಮ", "ಮಂಗಳ", "ಬುಧ", "ಗುರು", "ಶುಕ್ರ", "ಶನಿ" } };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, new string[] { "일", "월", "화", "수", "목", "금", "토" } };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, new string[] { "sk", "pr", "an", "tr", "kt", "pn", "št" } };
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, new string[] { "Svētd.", "Pirmd.", "Otrd.", "Trešd.", "Ceturtd.", "Piektd.", "Sestd." } };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, new string[] { "ഞായർ", "തിങ്കൾ", "ചൊവ്വ", "ബുധൻ", "വ്യാഴം", "വെള്ളി", "ശനി" } };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, new string[] { "रवि", "सोम", "मंगळ", "बुध", "गुरु", "शुक्र", "शनि" } };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, new string[] { "Ahd", "Isn", "Sel", "Rab", "Kha", "Jum", "Sab" } };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, new string[] { "søn.", "man.", "tir.", "ons.", "tor.", "fre.", "lør." } };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, new string[] { "zo", "ma", "di", "wo", "do", "vr", "za" } };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, new string[] { "niedz.", "pon.", "wt.", "śr.", "czw.", "pt.", "sob." } };
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, new string[] { "dom.", "seg.", "ter.", "qua.", "qui.", "sex.", "sáb." } };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, new string[] { "domingo", "segunda", "terça", "quarta", "quinta", "sexta", "sábado" } };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, new string[] { "dum.", "lun.", "mar.", "mie.", "joi", "vin.", "sâm." } };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, new string[] { "вс", "пн", "вт", "ср", "чт", "пт", "сб" } };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, new string[] { "ne", "po", "ut", "st", "št", "pi", "so" } };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, new string[] { "ned.", "pon.", "tor.", "sre.", "čet.", "pet.", "sob." } };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, new string[] { "нед", "пон", "уто", "сре", "чет", "пет", "суб" } };
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, new string[] { "ned", "pon", "uto", "sre", "čet", "pet", "sub" } };
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, new string[] { "sön", "mån", "tis", "ons", "tors", "fre", "lör" } };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, new string[] { "sön", "mån", "tis", "ons", "tors", "fre", "lör" } };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, new string[] { "Jumapili", "Jumatatu", "Jumanne", "Jumatano", "Alhamisi", "Ijumaa", "Jumamosi" } };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, new string[] { "ஞாயி.", "திங்.", "செவ்.", "புத.", "வியா.", "வெள்.", "சனி" } };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, new string[] { "ఆది", "సోమ", "మంగళ", "బుధ", "గురు", "శుక్ర", "శని" } };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, new string[] { "อา.", "จ.", "อ.", "พ.", "พฤ.", "ศ.", "ส." } };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, new string[] { "Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt" } };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, new string[] { "нд", "пн", "вт", "ср", "чт", "пт", "сб" } };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, new string[] { "CN", "Th 2", "Th 3", "Th 4", "Th 5", "Th 6", "Th 7" } };
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(AbbreviatedDayNames_Get_TestData_HybridGlobalization))]
        public void AbbreviatedDayNames_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string[] expected)
        {
            Assert.Equal(expected, format.AbbreviatedDayNames);
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
