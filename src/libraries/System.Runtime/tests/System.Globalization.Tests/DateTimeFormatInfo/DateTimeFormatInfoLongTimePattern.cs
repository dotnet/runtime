// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoLongTimePattern
    {
        [Fact]
        public void LongTimePattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("HH:mm:ss", DateTimeFormatInfo.InvariantInfo.LongTimePattern);
        }

        public static IEnumerable<object[]> LongTimePattern_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "H:mm:ss ч." };
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "HH.mm.ss" };
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "HH.mm.ss" };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "H.mm.ss" };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "HH:mm:ss" }; // H:mm:ss
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "H.mm.ss" };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "HH h mm min ss s" }; // HH 'h' mm 'min' ss 's'
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "hh:mm:ss tt" };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "HH.mm.ss" };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "hh:mm:ss tt" };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "tt h:mm:ss" };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("no").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "H:mm:ss" };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "tt h:mm:ss" };
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "tt h:mm:ss" };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "tt h:mm:ss" };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "h:mm:ss tt" };
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "HH:mm:ss" };
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "HH:mm:ss" }; // tth:mm:ss
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "tth:mm:ss" };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "tth:mm:ss" };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "tth:mm:ss" };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "tth:mm:ss" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(LongTimePattern_Get_TestData_HybridGlobalization))]
        public void LongTimePattern_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string value)
        {
            Assert.Equal(value, format.LongTimePattern);
        }

        public static IEnumerable<object[]> LongTimePattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "dddd, dd MMMM yyyy HH:mm:ss" };
            yield return new object[] { "HH" };
            yield return new object[] { "T" };
            yield return new object[] { "HH:mm:ss dddd, dd MMMM yyyy" };
            yield return new object[] { "HH:mm:ss" };
        }

        [Theory]
        [MemberData(nameof(LongTimePattern_Set_TestData))]
        public void LongTimePattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.LongTimePattern = value;
            Assert.Equal(value, format.LongTimePattern);
        }

        [Fact]
        public void LongTimePattern_Set_InvalidatesDerivedPatterns()
        {
            const string Pattern = "#$";
            var format = new DateTimeFormatInfo();
            var d = DateTimeOffset.Now;
            d.ToString("F", format); // FullDateTimePattern
            d.ToString("G", format); // GeneralLongTimePattern
            d.ToString(format); // DateTimeOffsetPattern
            format.LongTimePattern = Pattern;
            Assert.Contains(Pattern, d.ToString("F", format));
            Assert.Contains(Pattern, d.ToString("G", format));
            Assert.Contains(Pattern, d.ToString(format));
        }

        [Fact]
        public void LongTimePattern_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.LongTimePattern = null);
        }

        [Fact]
        public void LongTimePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.LongTimePattern = "HH:mm:ss");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        public void LongTimePattern_CheckReadingTimeFormatWithSingleQuotes_ICU()
        {
            // Usually fr-CA long time format has a single quotes e.g. "HH 'h' mm 'min' ss 's'".
            // Ensuring when reading such formats from ICU we'll not eat the spaces after the single quotes.
            string longTimeFormat = CultureInfo.GetCultureInfo("fr-CA").DateTimeFormat.LongTimePattern;
            int startIndex = 0;

            while ((startIndex = longTimeFormat.IndexOf('\'', startIndex)) >= 0 && startIndex < longTimeFormat.Length - 1)
            {
                // We have the opening single quote, find the closing one.
                startIndex++;
                if ((startIndex = longTimeFormat.IndexOf('\'', startIndex)) > 0 && startIndex < longTimeFormat.Length - 1)
                {
                    Assert.Equal(' ', longTimeFormat[++startIndex]);
                }
                else
                {
                    break; // done.
                }
            }
        }
    }
}
