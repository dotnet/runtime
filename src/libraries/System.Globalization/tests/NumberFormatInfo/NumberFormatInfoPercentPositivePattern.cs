// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentPositivePattern
    {
        public static IEnumerable<object[]> PercentPositivePattern_TestData()
        {
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, 1 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-MY").NumberFormat, 1 };
            yield return new object[] { CultureInfo.GetCultureInfo("tr").NumberFormat, 2 };            
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("cs-CZ").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-CH").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-DE").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-LI").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-LU").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("el-CY").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-AT").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-AU").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-DE").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-DM").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-FI").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-FJ").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-SE").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-SG").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("es-ES").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("et-EE").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("fi-FI").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("fil-PH").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("fr-BE").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("fr-CH").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("fr-FR").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("gu-IN").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("hr-BA").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("hu-HU").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("lt-LT").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("lv-LV").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("nb-NO").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("nl-AW").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("ro-RO").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("sr-Cyrl-RS").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("sv-AX").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("sw-CD").NumberFormat, 1 };
            }
        }

        /// <summary>
        /// Not testing for NLS as the culture data can change
        /// https://blogs.msdn.microsoft.com/shawnste/2005/04/05/culture-data-shouldnt-be-considered-stable-except-for-invariant/
        /// In the CultureInfoAll test class we are testing the expected behavior
        /// for NLS by enumerating all locales on the system and then test them.
        /// </summary>
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(PercentPositivePattern_TestData))]
        public void PercentPositivePattern_Get_ReturnsExpected_ICU(NumberFormatInfo format, int expected)
        {
            Assert.Equal(expected, format.PercentPositivePattern);
        }

        [Fact]
        public void PercentPositivePattern_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(0, NumberFormatInfo.InvariantInfo.PercentPositivePattern);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void PercentPositivePattern_Set_GetReturnsExpected(int newPercentPositivePattern)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentPositivePattern = newPercentPositivePattern;
            Assert.Equal(newPercentPositivePattern, format.PercentPositivePattern);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void PercentPositivePattern_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", "PercentPositivePattern", () => format.PercentPositivePattern = value);
        }

        [Fact]
        public void PercentPositivePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentPositivePattern = 1);
        }
    }
}
