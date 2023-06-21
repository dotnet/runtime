// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetEraName
    {
        public static IEnumerable<object[]> GetEraName_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, 1, "A.D." };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, 0, "A.D." };

            var enUSFormat = new CultureInfo("en-US").DateTimeFormat;
            yield return new object[] { enUSFormat, 1, DateTimeFormatInfoData.EnUSEraName() };
            yield return new object[] { enUSFormat, 0, DateTimeFormatInfoData.EnUSEraName() };

            var frRFormat = new CultureInfo("fr-FR").DateTimeFormat;
            yield return new object[] { frRFormat, 1, "ap. J.-C." };
            yield return new object[] { frRFormat, 0, "ap. J.-C." };

            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                for (int era = 0; era < 2; era++)
                {
                    yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, era, "\u0628\u0639\u062f\u0020\u0627\u0644\u0647\u062c\u0631\u0629" };
                    yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, era, "\u12d3\u002f\u121d" };
                    yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, era, "\u0441\u043b\u002e\u0425\u0440\u002e" };
                    yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, era, "\u0996\u09c3\u09b7\u09cd\u099f\u09be\u09ac\u09cd\u09a6" };
                    yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, era, "\u0996\u09c3\u09b7\u09cd\u099f\u09be\u09ac\u09cd\u09a6" };
                    yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, era, "dC" };
                    yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, era, "n. l." };
                    yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, era, "e.Kr." };
                    yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, era, "n. Chr." };
                    yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, era, "\u03bc\u002e\u03a7\u002e" };
                    yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, era, "AD" };
                    yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, era, "d. C." };
                    yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, era, "pKr" };
                    yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, era, "AP" };
                    yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, era, "jKr." };
                    yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, era, "ap. J.-C." };
                    yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, era, "\u0a88\u002e\u0ab8\u002e" };
                    yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, era, "\u05dc\u05e1\u05e4\u05d9\u05e8\u05d4" };
                    yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, era, "\u0908\u0938\u094d\u0935\u0940" };
                    yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, era, "po. Kr." };
                    yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, era, "i. sz." };
                    yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, era, "M" };
                    yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, era, "d.C." };
                    yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, era, "\u897f\u66a6" };
                    yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, era, "\u0c95\u0ccd\u0cb0\u0cbf\u002e\u0cb6" };
                    yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, era, "AD" };
                    yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, era, "po Kr." };
                    yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, era, "\u006d\u002e\u0113\u002e" };
                    yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, era, "\u0d0e\u0d21\u0d3f" };
                    yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, era, "\u0907\u002e\u0020\u0938\u002e" };
                    yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, era, "TM" };
                    yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, era, "e.Kr." };
                    yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, era, "n.Chr." };
                    yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, era, "n.e." };
                    yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, era, "d.Hr." };
                    yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, era, "\u043d\u002e\u0020\u044d\u002e" };
                    yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, era, "po Kr." };
                    yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, era, "BK" };
                    yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, era, "\u0b95\u0bbf\u002e\u0baa\u0bbf\u002e" };
                    yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, era, "\u0e1e\u002e\u0e28\u002e" };
                    yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, era, "Sau CN" };
                    yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, era, "\u516c\u5143" };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetEraName_TestData))]
        public void GetEraName_Invoke_ReturnsExpected(DateTimeFormatInfo format, int era, string expected)
        {
            Assert.Equal(expected, format.GetEraName(era));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void GetEraName_InvalidEra_ThrowsArgumentOutOfRangeException(int era)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("era", () => format.GetEraName(era));
        }
    }
}
