// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortestDayNames
    {
        [Fact]
        public void ShortestDayNames_InvariantInfo()
        {
            Assert.Equal(new string[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" }, DateTimeFormatInfo.InvariantInfo.ShortestDayNames);
        }

        [Fact]
        public void ShortestDayNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.ShortestDayNames, format.ShortestDayNames);
            Assert.NotSame(format.ShortestDayNames, format.ShortestDayNames);
        }

        public static IEnumerable<object[]> ShortestDayNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "" } };
        }

        public static IEnumerable<object[]> ShortestDayNames_Get_TestData_HybridGlobalization()
        {
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, new string[] { "ح", "ن", "ث", "ر", "خ", "ج", "س" } };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, new string[] { "እ", "ሰ", "ማ", "ረ", "ሐ", "ዓ", "ቅ" } };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, new string[] { "н", "п", "в", "с", "ч", "п", "с" } };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, new string[] { "র", "সো", "ম", "বু", "বৃ", "শু", "শ" } };
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, new string[] { "dg", "dl", "dt", "dc", "dj", "dv", "ds" } };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, new string[] { "N", "P", "Ú", "S", "Č", "P", "S" } };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, new string[] { "S", "M", "T", "O", "T", "F", "L" } };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, new string[] { "S", "M", "D", "M", "D", "F", "S" } };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, new string[] { "Κ", "Δ", "Τ", "Τ", "Π", "Π", "Σ" } };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, new string[] { "Su.", "M.", "Tu.", "W.", "Th.", "F.", "Sa." } };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, new string[] { "S", "M", "T", "W", "T", "F", "S" } };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, new string[] { "D", "L", "M", "M", "J", "V", "S" } };
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, new string[] { "D", "L", "M", "X", "J", "V", "S" } };
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, new string[] { "P", "E", "T", "K", "N", "R", "L" } };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, new string[] { "ی", "د", "س", "چ", "پ", "ج", "ش" } };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, new string[] { "S", "M", "T", "K", "T", "P", "L" } };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, new string[] { "Lin", "Lun", "Mar", "Miy", "Huw", "Biy", "Sab" } };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, new string[] { "D", "L", "M", "M", "J", "V", "S" } };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, new string[] { "ર", "સો", "મં", "બુ", "ગુ", "શુ", "શ" } };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, new string[] { "א׳", "ב׳", "ג׳", "ד׳", "ה׳", "ו׳", "ש׳" } };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, new string[] { "र", "सो", "मं", "बु", "गु", "शु", "श" } };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, new string[] { "N", "P", "U", "S", "Č", "P", "S" } };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, new string[] { "n", "p", "u", "s", "č", "p", "s" } };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, new string[] { "V", "H", "K", "Sz", "Cs", "P", "Sz" } };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, new string[] { "M", "S", "S", "R", "K", "J", "S" } };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, new string[] { "D", "L", "M", "M", "G", "V", "S" } };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, new string[] { "日", "月", "火", "水", "木", "金", "土" } };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, new string[] { "ಭಾ", "ಸೋ", "ಮಂ", "ಬು", "ಗು", "ಶು", "ಶ" } };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, new string[] { "일", "월", "화", "수", "목", "금", "토" } };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, new string[] { "S", "P", "A", "T", "K", "P", "Š" } };
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, new string[] { "S", "P", "O", "T", "C", "P", "S" } };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, new string[] { "ഞാ", "തി", "ചൊ", "ബു", "വ്യാ", "വെ", "ശ" } };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, new string[] { "A", "I", "S", "R", "K", "J", "S" } };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, new string[] { "S", "M", "T", "O", "T", "F", "L" } };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, new string[] { "Z", "M", "D", "W", "D", "V", "Z" } };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, new string[] { "N", "P", "W", "Ś", "C", "P", "S" } };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, new string[] { "D", "S", "T", "Q", "Q", "S", "S" } };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, new string[] { "D", "L", "M", "M", "J", "V", "S" } };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, new string[] { "В", "П", "В", "С", "Ч", "П", "С" } };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, new string[] { "n", "p", "u", "s", "š", "p", "s" } };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, new string[] { "n", "p", "t", "s", "č", "p", "s" } };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, new string[] { "н", "п", "у", "с", "ч", "п", "с" } };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, new string[] { "S", "M", "T", "W", "T", "F", "S" } };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, new string[] { "ஞா", "தி", "செ", "பு", "வி", "வெ", "ச" } };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, new string[] { "ఆ", "సో", "మ", "బు", "గు", "శు", "శ" } };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, new string[] { "อา", "จ", "อ", "พ", "พฤ", "ศ", "ส" } };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, new string[] { "P", "P", "S", "Ç", "P", "C", "C" } };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, new string[] { "Н", "П", "В", "С", "Ч", "П", "С" } };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, new string[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" } };
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, new string[] { "日", "一", "二", "三", "四", "五", "六" } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(ShortestDayNames_Get_TestData_HybridGlobalization))]
        public void ShortestDayNames_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string[] expected)
        {
            Assert.Equal(expected, format.ShortestDayNames);
        }

        [Theory]
        [MemberData(nameof(ShortestDayNames_Set_TestData))]
        public void ShortestDayNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.ShortestDayNames = value;
            Assert.Equal(value, format.ShortestDayNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.ShortestDayNames);
            Assert.Equal(value, format.ShortestDayNames);
        }

        [Fact]
        public void ShortestDayNames_SetNulValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortestDayNames = null);
        }

        [Fact]
        public void ShortestDayNames_SetNulValueInValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortestDayNames = new string[] { "1", "2", "3", null, "5", "6", "7" });
        }

        public static IEnumerable<object[]> ShortestDayNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Sun" } };
            yield return new object[] { new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(ShortestDayNames_SetInvalidLength_TestData))]
        public void ShortestDayNames_SetInvalidLength_ThrowsArgumentException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", (() => format.ShortestDayNames = value));
        }

        [Fact]
        public void ShortestDayNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortestDayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" });
        }
    }
}
