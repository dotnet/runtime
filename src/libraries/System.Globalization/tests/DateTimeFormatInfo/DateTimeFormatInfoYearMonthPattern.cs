// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoYearMonthPattern
    {
        public static IEnumerable<object[]> YearMonthPattern_Get_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "yyyy MMMM" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "MMMM yyyy" };
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                // see the comments on the right to check the non-Hybrid result, if it differs
                yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "MMMM yyyy" }; // "MMMM yyyy g"
                yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "MMMM yyyy \u0433." }; // ICU: "MMMM yyyy '\u0433'."
                yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "MMMM de yyyy" }; // ICU:  "MMMM 'de' yyyy"
                yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "MMMM de yyyy" }; // ICU:  "MMMM 'de' yyyy"
                yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "MMMM de yyyy" }; // ICU:  "MMMM 'de' yyyy"
                yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "MMMM de yyyy" }; // ICU:  "MMMM 'de' yyyy"
                yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "yyyy MMMM" };
                yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "MMMM yyyy." };
                yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "yyyy. MMMM" };
                yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "yyyy\u5e74M\u6708" };
                yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "yyyy\ub144 MMMM" };
                yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "yyyy m. MMMM" }; // ICU: "yyyy 'm'. MMMM"
                yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "yyyy. g. MMMM" }; // ICU: "yyyy. 'g'. MMMM"
                yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "yyyy MMMM" };
                yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "MMMM de yyyy" }; // ICU:  "MMMM 'de' yyyy"
                yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "MMMM yyyy \u0433." }; // ICU: "MMMM yyyy '\u0433'."
                yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "MMMM yyyy." };
                yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "MMMM n\u0103m yyyy" }; // ICU: "MMMM 'n\u0103m' yyyy"
                yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "yyyy\u5e74M\u6708" };
                yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "yyyy\u5e74M\u6708" };
            }
        }

        [Theory]
        [MemberData(nameof(YearMonthPattern_Get_TestData))]
        public void YearMonthPattern(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.YearMonthPattern);
        }

        public static IEnumerable<object[]> YearMonthPattern_Set_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "garbage" };
            yield return new object[] { "yyyy MMMM" };
            yield return new object[] { "y" };
            yield return new object[] { "Y" };
        }

        [Theory]
        [MemberData(nameof(YearMonthPattern_Set_TestData))]
        public void YearMonthPattern_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.YearMonthPattern = value;
            Assert.Equal(value, format.YearMonthPattern);
        }

        [Fact]
        public void YearMonthPattern_SetNull_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.YearMonthPattern = null);
        }

        [Fact]
        public void YearMonthPattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.YearMonthPattern = "yyyy MMMM"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
